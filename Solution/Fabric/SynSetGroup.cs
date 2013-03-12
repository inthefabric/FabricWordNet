using System.Collections.Generic;
using Fabric.Apps.WordNet.Data.Domain;
using LAIR.Collections.Generic;
using NHibernate;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public class SynSetGroup {

		private static Dictionary<string, Synset> SynsetCache;
		private static Dictionary<string, Word> WordCache;

		private readonly string vRootWord;
		private readonly Set<SynSet> vGroup;
		
		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SynSetGroup(string pRootWord, Set<SynSet> pGroup) {
			vRootWord = pRootWord;
			vGroup = pGroup;

			if ( SynsetCache == null ) {
				SynsetCache = new Dictionary<string, Synset>();
				WordCache = new Dictionary<string, Word>();
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void InsertSynSetsAndWords(ISession pSess) {
			foreach ( SynSet ss in vGroup ) {
				InsertSynSet(pSess, ss);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void InsertSynSet(ISession pSess, SynSet pSynSet) {
			if ( SynsetCache.ContainsKey(pSynSet.ID) ) {
				return;
			}

			var dbSyn = new Synset();
			dbSyn.SsId = pSynSet.ID;
			dbSyn.PartOfSpeechId = (byte)pSynSet.POS;
			dbSyn.Gloss = pSynSet.Gloss;
			pSess.Save(dbSyn);
			SynsetCache.Add(pSynSet.ID, dbSyn);

			foreach ( string word in pSynSet.Words ) {
				if ( WordCache.ContainsKey(pSynSet.ID+"|"+word) ) {
					continue;
				}

				var dbWord = new Word();
				dbWord.Name = word;
				dbWord.SynSet = dbSyn;
				pSess.Save(dbWord);
				WordCache.Add(pSynSet.ID+"|"+word, dbWord);
			}
		}

		

		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static int GetCachedSynsetCount() {
			return SynsetCache.Keys.Count;
		}

		/*--------------------------------------------------------------------------------------------*/
		public static bool InsertLexicalsAndSemantics(ISession pSess, WordNetEngine pEngine,
																			int pStart, int pCount) {
			int i = 0;

			foreach ( string ssId in SynsetCache.Keys ) {
				if ( i++ < pStart ) {
					continue;
				}

				if ( i > pStart+pCount ) {
					break;
				}

				InsertLexAndSemForSynSet(pSess, ssId, pEngine.GetSynSet(ssId));
			}

			return (i < SynsetCache.Keys.Count);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void InsertLexAndSemForSynSet(ISession pSess, string pSynSetId, SynSet pSynSet) {
			Synset dbSynSet = SynsetCache[pSynSetId];

			Dictionary<WordNetEngine.SynSetRelation, Dictionary<string, Set<string>>> lexMap = 
				pSynSet.GetLexicallyRelatedWords();

			foreach ( WordNetEngine.SynSetRelation rel in lexMap.Keys ) {
				var setDict = lexMap[rel];
				
				foreach ( string setDictKey in setDict.Keys ) {
					var strSet = setDict[setDictKey];
					
					foreach ( string s in strSet ) {
						var dbLex = new Lexical();
						dbLex.SynSet = dbSynSet;
						dbLex.RelationId = (byte)rel;
						dbLex.Word = setDictKey;
						dbLex.RelatedWord = (setDictKey == s ? null : s);
						pSess.Save(dbLex);
					}
				}
			}

			foreach ( WordNetEngine.SynSetRelation rel in pSynSet.SemanticRelations ) {
				Set<SynSet> relSet = pSynSet.GetRelatedSynSets(rel, false);

				foreach ( SynSet rs in relSet ) {
					var dbSem = new Semantic();
					dbSem.SynSet = dbSynSet;
					dbSem.RelationId = (byte)rel;
					//if ( !SynsetCache.ContainsKey(rs.ID) ) { continue; } //TEST
					dbSem.TargetSynSet = SynsetCache[rs.ID];
					pSess.Save(dbSem);
				}
			}
		}
		
	}
	
}