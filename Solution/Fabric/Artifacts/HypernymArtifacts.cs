using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
		private void BuildArtifacts(IEnumerable<TreeNode> pNodes) {
			foreach ( TreeNode n in pNodes ) {
				Synset ss = n.SynSet;

				if ( vInsertMap.Contains("s"+ss.Id) ) {
					continue;
				}

				vInsertMap.Add("s"+ss.Id);
				List<string> words = GetWordList(ss);
				string pos = Stats.PartsOfSpeech[ss.PartOfSpeechId]+"";

				Artifact art = new Artifact();
				art.Name = string.Join(", ", words);
				art.Note = pos+": "+ss.Gloss;
				art.Synset = ss;
				art.Word = (words.Count == 1 ? ss.WordList[0] : null);
				vList.Add(new HypArt { Art = art, Node = n, Word = art.Word });

				if ( art.Word != null ) {
					continue;
				}

				for ( int i = 0 ; i < words.Count ; ++i ) {
					Word w = ss.WordList[i];

					if ( vInsertMap.Contains("w"+w.Id) ) {
						continue;
					}

					vInsertMap.Add("w"+w.Id);

					art = new Artifact();
					art.Name = words[i];
					art.Note = pos+": "+ss.Gloss;
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

			d = Regex.Replace(d, "; \"(.*?)\"", "");

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
		public bool IsWord { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void SetDisambLevel(int pLevel, HypernymTree pTree, ISession pSess) {
			if ( pLevel <= 12 ) {
				int lev = pLevel-1;
				int depth = (lev/2)/3+1;
				int width = (lev/2)%3+1;
				int holos = (lev%2 == 1 ? width : 0);

				Art.Disamb = (IsWord ?
					GetWordHyperDisamb(Node, Art.Name, width, depth) :
					GetSynsetHyperDisamb(Node, Art.Name, width, depth));

				if ( Node.SynSet.Gloss.IndexOf('(', 0, 1) == 0 ) {
					int endI = Node.SynSet.Gloss.IndexOf(')');
					Art.Disamb += " "+Node.SynSet.Gloss.Substring(0, endI+1);
				}

				if ( holos > 0 ) {
					string holo = GetHolonymDisamb(pSess, Node, Art.Name, pTree, holos);
					
					if ( holo.Length > 0 ) {
						Art.Disamb += "; ["+holo+"]";
					}
				}
			}

			/*if ( pLevel == 12 ) {
				Console.WriteLine(" - "+Art.Name+" // "+
					(IsWord ? "w"+Word.Id : "s"+Node.SynSet.Id)+" // "+Art.Disamb);
			}*/

			switch ( pLevel ) {
				case 13:
					Art.Disamb = HypernymArtifacts.GlossToDisamb(Node.SynSet.Gloss, true);
					//Console.WriteLine("Disamb "+(IsWord ? "w"+Word.Id : "s"+Node.SynSet.Id)+": "+
					//	Art.Name+" // "+Art.Disamb);
					break;

				case 14:
					Art.Disamb = HypernymArtifacts.GlossToDisamb(Node.SynSet.Gloss, false);
					//Console.WriteLine("FullDisamb "+(IsWord ? "w"+Word.Id : "s"+Node.SynSet.Id)+": "+
					//	Art.Name+" // "+Art.Disamb);
					break;
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private string GetSynsetHyperDisamb(TreeNode pNode, string pSkipWord, int pWidth, int pDepth) {
			if ( pNode.Hypernyms.Count == 0 ) {
				return null;
			}

			string d = "";

			foreach ( TreeNode hn in pNode.Hypernyms ) {
				d += (d == "" ? "" : "; ")+GetWordHyperDisamb(hn, pSkipWord, pWidth, pDepth-1);
			}

			if ( pDepth > 1 ) {
				d += " > ";
				int count = 0;

				foreach ( TreeNode hn in pNode.Hypernyms ) {
					string hd = GetSynsetHyperDisamb(hn, pSkipWord, pWidth, pDepth-1);

					if ( hd == null ) {
						continue;
					}

					d += (count == 0 ? "" : "; ")+hd;

					if ( ++count >= pWidth ) {
						break;
					}
				}
			}

			return d;
		}

		/*--------------------------------------------------------------------------------------------*/
		private string GetWordHyperDisamb(TreeNode pNode, string pSkipWord, int pWidth, int pDepth) {
			var dList = new List<string>();
			List<string> words = HypernymArtifacts.GetWordList(pNode.SynSet);

			for ( int i = 0 ; i < words.Count && dList.Count < pWidth ; ++i ) {
				if ( words[i] == pSkipWord ) {
					continue;
				}

				dList.Add(words[i]);
			}

			string d = string.Join(", ", dList);

			if ( pDepth > 1 ) {
				d += " > "+GetSynsetHyperDisamb(pNode, pSkipWord, pWidth, pDepth-1);
			}

			return d;
		}

		/*--------------------------------------------------------------------------------------------*/
		private string GetHolonymDisamb(ISession pSess, TreeNode pNode, string pSkipWord, 
																	HypernymTree pTree, int pWidth) {
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

			if ( !HolonymMap.ContainsKey(pNode.SynSet.Id) ) {
				return "";
			}

			var dList = new List<string>();
			var words = new List<string>();

			foreach ( int targSsId in HolonymMap[pNode.SynSet.Id] ) {
				words.AddRange(HypernymArtifacts.GetWordList(pTree.SynMap[targSsId]));
			}

			for ( int i = 0 ; i < words.Count && dList.Count < pWidth ; ++i ) {
				if ( words[i] == pSkipWord ) {
					continue;
				}

				dList.Add(words[i]);
			}

			return string.Join(", ", dList);
		}

	}

}