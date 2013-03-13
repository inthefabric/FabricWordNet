using System;
using System.Collections.Generic;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public static class BuildArtifacts {

		//SELECT Name, Disamb, COUNT(Name) FROM Artifact GROUP BY Name, Disamb HAVING COUNT(Name) > 1 ORDER BY COUNT(Name) DESC


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void InsertWordAndSynsetArtifacts(ISession pSess) {
			BuildWordNet.SetDbStateBeforeBatchInsert(pSess);
			pSess.CreateSQLQuery("DELETE FROM "+typeof(Artifact).Name+" WHERE 1=1").UniqueResult();
			pSess.CreateSQLQuery("VACUUM").UniqueResult();
			InsertWithHypernymTree(pSess);
			BuildWordNet.SetDbStateAfterBatchInsert(pSess);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void InsertWithHypernymTree(ISession pSess) {
			var ht = new HypernymTree(pSess);
			List<TreeNode> nodes = ht.GetSortedNodeList();
			var insertMap = new HashSet<string>();
			int total = nodes.Count;
			Console.WriteLine("HypernymTree nodes: "+total);

			while ( nodes.Count > 0 ) {
				using ( ITransaction tx = pSess.BeginTransaction() ) {
					int count = Math.Min(5000, nodes.Count);
					InsertWithHypernymTreeNodes(pSess, nodes.GetRange(0, count), insertMap);
					nodes.RemoveRange(0, count);
					tx.Commit();
					Console.WriteLine("Committed "+(total-nodes.Count)+" of "+total+".");
				}
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void InsertWithHypernymTreeNodes(ISession pSess, List<TreeNode> pNodes,
																		HashSet<string> pInsertMap) {
			foreach ( TreeNode n in pNodes ) {
				Synset ss = n.SynSet;

				if ( pInsertMap.Contains("s"+ss.Id) ) {
					continue;
				}

				pInsertMap.Add("s"+ss.Id);
				string pos = Stats.PartsOfSpeech[ss.PartOfSpeechId]+"";
				List<string> words = GetWordList(ss);

				/*if ( n.Hypernyms.Count > 0 ) {
					ssDisamb += ": ";
					bool first = true;

					foreach ( TreeNode hn in n.Hypernyms ) {
						ssDisamb += (first ? "" : "; ")+string.Join(", ", GetWordList(hn.SynSet));
						first = false;
					}

					/*if ( n.Hypernyms.Count > 1 ) {
						Console.WriteLine("... multi-hypernym: "+
							n.SynSet.Id+": "+string.Join("; ", words)+"\n      - "+ssDisamb);
					}* /
				}*/

				Artifact art = new Artifact();
				art.Name = string.Join(", ", words);
				art.Disamb = pos+": "+GlossToDisamb(ss.Gloss);
				art.Note = GlossToNote(ss.Gloss);
				art.Synset = ss;
				art.Word = (words.Count == 1 ? ss.WordList[0] : null);
				//Console.WriteLine("ARTs: "+art.Name+" / "+art.Disamb);
				pSess.Save(art);

				if ( words.Count == 1 ) {
					continue;
				}

				for ( int i = 0 ; i < words.Count ; ++i ) {
					Word w = ss.WordList[i];

					if ( pInsertMap.Contains("w"+w.Id) ) {
						continue;
					}

					pInsertMap.Add("w"+w.Id);

					/*var disamb = new List<string>();

					for ( int j = 0 ; j < words.Count && disamb.Count < 4 ; ++j ) {
						if ( i == j ) {
							continue;
						}

						disamb.Add(words[j]);
					}*/

					art = new Artifact();
					art.Name = words[i];
					art.Disamb = pos+": "+GlossToDisamb(ss.Gloss);
					art.Note = GlossToNote(ss.Gloss);
					art.Word = w;
					//Console.WriteLine("ARTw: "+art.Name+" / "+art.Disamb);
					pSess.Save(art);
				}
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static List<String> GetWordList(Synset pSynset) {
			var words = new List<string>();

			foreach ( Word w in pSynset.WordList ) {
				words.Add(FixWordName(w));
			}

			return words;
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string FixWordName(Word pWord) {
			return pWord.Name.Replace('_', ' ');
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string GlossToDisamb(string pGloss) {
			string d = pGloss+"";
			int endI = d.IndexOf(";");

			if ( endI != -1 ) {
				d = d.Substring(0, endI);
			}

			if ( d.IndexOf("the ") == 0 ) {
				d = d.Substring(4);
			}

			if ( d.IndexOf("a ") == 0 ) {
				d = d.Substring(2);
			}

			if ( d.IndexOf("an ") == 0 ) {
				d = d.Substring(3);
			}

			return d;
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string GlossToNote(string pGloss) {
			string d = pGloss+"";
			int endI = pGloss.IndexOf(";");
			return (endI != -1 ? d.Substring(endI+2) : null);
		}

	}

}