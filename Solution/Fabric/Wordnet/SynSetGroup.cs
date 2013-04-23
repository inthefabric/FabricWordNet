using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;
using LAIR.Collections.Generic;
using NHibernate;

namespace Fabric.Apps.WordNet.Wordnet {

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
				dbWord.Synset = dbSyn;
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
			List<string> keys = SynsetCache.Keys.ToList();
			int i = pStart;

			for (  ; i < pStart+pCount ; ++i ) {
				if ( i >= keys.Count ) {
					break;
				}

				InsertLexAndSemForSynSet(pSess, keys[i], pEngine.GetSynSet(keys[i]));
			}

			return (i < keys.Count);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void InsertLexAndSemForSynSet(ISession pSess, string pSynSetId, SynSet pSynSet) {
			Synset dbSynSet = SynsetCache[pSynSetId];
			List<LexicalRelation> lexRels = pSynSet.GetLexicallyRelated();

			foreach ( LexicalRelation lr in lexRels ) {
				var dbLex = new Lexical();
				dbLex.Synset = dbSynSet;
				dbLex.Word = WordCache[dbLex.Synset.SsId+"|"+lr.FromWord];
				dbLex.RelationId = (byte)lr.Relation;
				dbLex.TargetSynset = SynsetCache[lr.ToSyn.ID];
				dbLex.TargetWord = WordCache[dbLex.TargetSynset.SsId+"|"+lr.ToWord];
				pSess.Save(dbLex);
			}

			foreach ( WordNetEngine.SynSetRelation rel in pSynSet.SemanticRelations ) {
				Set<SynSet> relSet = pSynSet.GetRelatedSynSets(rel, false);

				foreach ( SynSet rs in relSet ) {
					var dbSem = new Semantic();
					dbSem.Synset = dbSynSet;
					dbSem.RelationId = (byte)rel;
					dbSem.TargetSynset = SynsetCache[rs.ID];
					pSess.Save(dbSem);
				}
			}
		}
		
	}
	
}