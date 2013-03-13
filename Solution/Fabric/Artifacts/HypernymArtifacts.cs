using System;
using System.Collections.Generic;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet.Artifacts {

	/*================================================================================================*/
	public class HypernymArtifacts {

		//SELECT Name, Disamb, COUNT(Name) FROM Artifact GROUP BY Name, Disamb HAVING COUNT(Name) > 1 ORDER BY COUNT(Name) DESC

		private readonly HypernymTree vTree;
		private readonly HashSet<string> vInsertMap;
		private readonly List<HypArt> vList;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public HypernymArtifacts(ISession pSess) {
			vTree = new HypernymTree(pSess);
			List<TreeNode> nodes = vTree.GetSortedNodeList();

			int total = nodes.Count;
			Console.WriteLine("HypernymTree nodes: "+total);
			
			vInsertMap = new HashSet<string>();
			vList = new List<HypArt>();

			BuildArtifacts(nodes);
			ResolveArtifactDuplicates(pSess);
			Console.WriteLine("HypernymTree Artifacts: "+total);

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				foreach ( HypArt ha in vList ) {
					pSess.Save(ha.Art);
				}

				tx.Commit();
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void BuildArtifacts(List<TreeNode> pNodes) {
			foreach ( TreeNode n in pNodes ) {
				Synset ss = n.SynSet;

				if ( vInsertMap.Contains("s"+ss.Id) ) {
					continue;
				}

				vInsertMap.Add("s"+ss.Id);
				List<string> words = GetWordList(ss);

				Artifact art = new Artifact();
				art.Name = string.Join(", ", words);
				art.Note = ss.Gloss;
				art.Synset = ss;
				art.Word = (words.Count == 1 ? ss.WordList[0] : null);
				vList.Add(new HypArt { Art = art, Node = n, Word = art.Word });

				for ( int i = 0 ; words.Count > 1 && i < words.Count ; ++i ) {
					Word w = ss.WordList[i];

					if ( vInsertMap.Contains("w"+w.Id) ) {
						continue;
					}

					vInsertMap.Add("w"+w.Id);

					art = new Artifact();
					art.Name = words[i];
					art.Note = ss.Gloss;
					art.Word = w;
					vList.Add(new HypArt { Art = art, Node = n, Word = art.Word });
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
			int level = 1;

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
					}
				}

				Console.WriteLine("Duplicate count at level "+(level-1)+": "+dupCount);

				if ( dupCount == 0 ) {
					break;
				}

				level++;
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		internal static List<String> GetWordList(Synset pSynset) {
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
		internal static string GlossToDisamb(string pGloss, bool pTruncate) {
			string d = pGloss+"";
			int endI = d.IndexOf(";");

			if ( pTruncate && endI != -1 ) {
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

	}

	
	/*================================================================================================*/
	public class HypArt {

		private static IList<object[]> HolonymPairs;
		private static Dictionary<int, List<int>> HolonymMap;

		public Artifact Art { get; set; }
		public TreeNode Node { get; set; }
		public Word Word { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void SetDisambLevel(int pLevel, HypernymTree pTree, ISession pSess) {
			string pos = Stats.PartsOfSpeech[Node.SynSet.PartOfSpeechId]+"";

			switch ( pLevel ) {
				case 1:
					Art.Disamb = pos+": "+(Art.Word == null ?
						GetSynsetHyperDisamb() : GetWordHyperDisamb());
					break;

				case 2:
					Art.Disamb = pos+": "+(Art.Word == null ?
						GetSynsetHyperDisamb() : GetWordHyperDisamb())+"; "+
						GetHolonymDisamb(pSess, pTree);
					break;

				case 3:
					Art.Disamb = pos+": "+HypernymArtifacts.GlossToDisamb(Node.SynSet.Gloss, true);
					break;

				case 4:
					Art.Disamb = pos+": "+HypernymArtifacts.GlossToDisamb(Node.SynSet.Gloss, false);
					Console.WriteLine("BAD: "+Node.SynSet.Id+" / "+Art.Disamb);
					break;
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private string GetSynsetHyperDisamb() {
			if ( Node.Hypernyms.Count == 0 ) {
				return null;
			}

			string d = "";

			foreach ( TreeNode hn in Node.Hypernyms ) {
				d += (d == "" ? "" : "; ")+string.Join(", ", HypernymArtifacts.GetWordList(hn.SynSet));
			}

			return d;
		}

		/*--------------------------------------------------------------------------------------------*/
		private string GetWordHyperDisamb() {
			var d = new List<string>();
			List<string> words = HypernymArtifacts.GetWordList(Node.SynSet);

			for ( int j = 0 ; j < words.Count && d.Count < 4 ; ++j ) {
				string word = words[j];

				if ( word == Art.Name ) {
					continue;
				}

				d.Add(words[j]);
			}

			return string.Join(", ", words);
		}

		/*--------------------------------------------------------------------------------------------*/
		private string GetHolonymDisamb(ISession pSess, HypernymTree pTree) {
			if ( HolonymPairs == null ) {
				HolonymPairs = pSess.QueryOver<Semantic>()
					.Where(x =>
						x.RelationId == (byte)WordNetEngine.SynSetRelation.MemberHolonym ||
						x.RelationId == (byte)WordNetEngine.SynSetRelation.SubstanceHolonym ||
						x.RelationId == (byte)WordNetEngine.SynSetRelation.PartHolonym
					)
					.JoinQueryOver(x => x.SynSet)
					.SelectList(list => list
						.Select(x => x.SynSet.Id)
						.Select(x => x.TargetSynSet.Id)
					)
					.List<object[]>();

				HolonymMap = new Dictionary<int, List<int>>();

				foreach ( object[] pair in HolonymPairs ) {
					int key = (int)pair[0];

					if ( !HolonymMap.ContainsKey(key) ) {
						HolonymMap.Add(key, new List<int>());
					}

					HolonymMap[key].Add((int)pair[1]);
				}
			}

			if ( !HolonymMap.ContainsKey(Node.SynSet.Id) ) {
				return "";
			}

			var d = new List<string>();
			var words = new List<string>();

			foreach ( int targSsId in HolonymMap[Node.SynSet.Id] ) {
				words.AddRange(HypernymArtifacts.GetWordList(pTree.SynMap[targSsId]));
			}

			for ( int j = 0 ; j < words.Count && d.Count < 4 ; ++j ) {
				string word = words[j];

				if ( word == Art.Name ) {
					continue;
				}

				d.Add(words[j]);
			}

			return string.Join(", ", d);
		}

	}

}