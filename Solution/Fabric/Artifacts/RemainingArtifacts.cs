using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet.Artifacts {

	/*================================================================================================*/
	public class RemainingArtifacts {

		private readonly HypernymTree vTree;
		private readonly List<HypArt> vList;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public RemainingArtifacts(HypernymTree pTree, ISession pSess) {
			vTree = pTree;

			Console.WriteLine("Getting all used SynsetIds...");
			IList<int> artSynIdList = pSess.QueryOver<Artifact>()
				.Where(x => x.Synset != null)
				.Select(x => x.Synset.Id)
				.List<int>();
			Console.WriteLine("Found "+artSynIdList.Count+" used SynsetIds");

			Console.WriteLine("Getting all used WordIds...");
			IList<int> artWordIdList = pSess.QueryOver<Artifact>()
				.Where(x => x.Word != null)
				.Select(x => x.Word.Id)
				.List<int>();
			Console.WriteLine("Found "+artWordIdList.Count+" used WordIds");

			Dictionary<int,int> artSynIdMap = artSynIdList.ToDictionary(key => key);
			Dictionary<int,int> artWordIdMap = artWordIdList.ToDictionary(key => key);
			var remNodes = new List<TreeNode>();

			foreach ( int key in vTree.SynMap.Keys ) {
				Synset ss = vTree.SynMap[key];

				if ( artSynIdMap.ContainsKey(ss.Id) ) {
					continue;
				}

				if ( !vTree.NodeMap.ContainsKey(key) ) {
					Console.WriteLine("No TreeNode for "+key+" / "+ss.Gloss);
					remNodes.Add(new TreeNode(ss));
				}
				else {
					remNodes.Add(vTree.NodeMap[key]);
				}
			}

			Console.WriteLine("Removed "+(vTree.SynMap.Keys.Count-remNodes.Count)+" Synsets");

			foreach ( TreeNode n in remNodes ) {
				foreach ( Word w in n.SynSet.WordList ) {
					if ( artWordIdMap.ContainsKey(w.Id) ) {
						Console.WriteLine(" - Warning: Word "+w.Id+" / "+w.Name+" was already added");
					}
				}
			}

			int total = remNodes.Count;
			Console.WriteLine("Remaining Nodes: "+total);

			vList = new List<HypArt>();

			BuildArtifacts(remNodes);
			ResolveArtifactDuplicates(pSess);
			Console.WriteLine("Remaining Artifacts: "+vList.Count);

			/*using ( ITransaction tx = pSess.BeginTransaction() ) {
				foreach ( HypArt ha in vList ) {
					pSess.Save(ha.Art);
				}

				tx.Commit();
			}*/
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private void BuildArtifacts(IEnumerable<TreeNode> pNodes) {
			foreach ( TreeNode n in pNodes ) {
				Synset ss = n.SynSet;
				List<string> words = HypernymArtifacts.GetWordList(ss);
				string pos = Stats.PartsOfSpeech[n.SynSet.PartOfSpeechId]+"";

				Artifact art = new Artifact();
				art.Name = string.Join(", ", (words.Count > 5 ? words.GetRange(0, 5) : words));
				art.Name = HypernymArtifacts.TruncateString(art.Name, 128);
				art.Note = HypernymArtifacts.TruncateString(pos+": "+ss.Gloss, 256);
				art.Synset = ss;
				art.Word = (words.Count == 1 ? ss.WordList[0] : null);
				vList.Add(new HypArt { Art = art, Node = null, Word = art.Word });

				if ( art.Word != null ) {
					continue;
				}

				for ( int i = 0 ; i < words.Count ; ++i ) {
					Word w = ss.WordList[i];

					art = new Artifact();
					art.Name = HypernymArtifacts.TruncateString(words[i], 128);
					art.Note = HypernymArtifacts.TruncateString(pos+": "+ss.Gloss, 256);
					art.Word = w;
					vList.Add(new HypArt { Art = art, Node = n, Word = art.Word, IsWord = true });
				}
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private Dictionary<string, List<HypArt>> GetDuplicateMap() {
			var map = new Dictionary<string, List<HypArt>>();

			foreach ( HypArt ha in vList ) {
				string key = ha.Art.Name+"||"+ha.Art.Disamb;

				if ( !map.ContainsKey(key) ) {
					map.Add(key, new List<HypArt>());
				}

				map[key].Add(ha);
			}

			return map;
		}

		/*--------------------------------------------------------------------------------------------*/
		private void ResolveArtifactDuplicates(ISession pSess) {
			int level = 0;

			while ( true ) {
				Dictionary<string, List<HypArt>> dupMap = GetDuplicateMap();
				int dupCount = 0;

				foreach ( string key in dupMap.Keys ) {
					List<HypArt> haList = dupMap[key];

					if ( haList.Count == 1 ) {
						continue;
					}

					dupCount++;

					foreach ( HypArt ha in haList ) {
						ha.SetDisambLevel(level, vTree, pSess);
						ha.Art.Disamb = HypernymArtifacts.TruncateString(ha.Art.Disamb, 128);
					}
				}

				Console.WriteLine("Duplicate count at level "+level+": "+dupCount);

				if ( dupCount == 0 ) {
					break;
				}

				level++;
			}
		}
	}

}