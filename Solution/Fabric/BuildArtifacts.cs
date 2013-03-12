using System;
using System.Collections.Generic;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public static class BuildArtifacts {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void InsertWordAndSynsetArtifacts(ISession pSess) {
			BuildWordNet.SetDbStateBeforeBatchInsert(pSess);

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				InsertWithHypernymTree(pSess);
				Console.WriteLine("Committing...");
				tx.Commit();
				Console.WriteLine("Commit complete.");
			}

			BuildWordNet.SetDbStateAfterBatchInsert(pSess);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void InsertWithHypernymTree(ISession pSess) {
			var ht = new HypernymTree(pSess);
			List<TreeNode> nodes = ht.GetSortedNodeList();
			int total = nodes.Count;
			int count = 0;

			foreach ( TreeNode n in nodes ) {
				Synset ss = n.SynSet;
				string pos = Stats.PartsOfSpeech[ss.PartOfSpeechId]+"";
				var words = new List<string>();

				foreach ( Word w in ss.WordList ) {
					words.Add(FixWordName(w));
				}

				Artifact art = new Artifact();
				art.Name = string.Join("; ", words);
				art.Disamb = pos;
				art.Note = ss.Gloss;
				pSess.Save(art);

				ss.CreatedArtifact = art;
				pSess.SaveOrUpdate(ss);

				for ( int i = 0 ; i < words.Count ; ++i ) {
					var disamb = new List<string>();

					for ( int j = 0 ; j < words.Count && disamb.Count < 4 ; ++j ) {
						if ( i == j ) {
							continue;
						}

						disamb.Add(words[j]);
					}

					art = new Artifact();
					art.Name = words[i];
					art.Note = ss.Gloss;
					art.Disamb = string.Join("; ", disamb);
					pSess.Save(art);

					Word w = ss.WordList[i];
					w.CreatedArtifact = art;
					pSess.SaveOrUpdate(w);
				}

				count++;
				Console.WriteLine(" - "+count+" of "+total);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string FixWordName(Word pWord) {
			return pWord.Name.Replace('_', ' ');
		}

	}

}