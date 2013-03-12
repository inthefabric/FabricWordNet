using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;
using NHibernate;
using NHibernate.Transform;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class HypernymTree {

		private readonly Dictionary<int, Synset> vSynMap;
		private readonly Dictionary<int, TreeNode> vNodeMap;

		public TreeNode Root { get; private set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public HypernymTree(ISession pSess) {
			Console.WriteLine("HYPERNYM TREE");
			Console.WriteLine(" - Getting all 'Hypernym' Semantics...");
			IList<Semantic> hyperList = pSess.QueryOver<Semantic>()
				.Where(x => x.RelationId == (byte)WordNetEngine.SynSetRelation.Hypernym)
				.List();

			Console.WriteLine(" - Found "+hyperList.Count+" Semantics");
			Console.WriteLine(" - Getting all Synsets, filled with Words...");
			IList<Synset> synList = pSess.QueryOver<Synset>()
				.Fetch(x => x.WordList).Eager
				.TransformUsing(Transformers.DistinctRootEntity)
				.List();

			Console.WriteLine(" - Found "+synList.Count+" Synsets");
			Console.WriteLine(" - Building Synset and TreeNode maps...");
			vSynMap = synList.ToDictionary(ss => ss.Id);
			vNodeMap = new Dictionary<int, TreeNode>();

			foreach ( Semantic hyper in hyperList ) {
				int id = hyper.SynSet.Id;
				int tid = hyper.TargetSynSet.Id;
				bool hasN = vNodeMap.ContainsKey(id);
				bool hasTn = vNodeMap.ContainsKey(tid);

				TreeNode n = (hasN ? vNodeMap[id] : new TreeNode(vSynMap[id]));
				TreeNode tn = (hasTn ? vNodeMap[tid] : new TreeNode(vSynMap[tid]));

				if ( !hasN ) {
					vNodeMap.Add(id, n);
				}

				if ( !hasTn ) {
					vNodeMap.Add(tid, tn);
				}

				n.Hypernyms.Add(tn);
				tn.Hyponyms.Add(n);
			}

			Root = vNodeMap[59724];
		}

		/*--------------------------------------------------------------------------------------------*/
		public List<TreeNode> GetSortedNodeList() {
			Console.WriteLine(" - Building sorted TreeNode list...");

			var queue = new List<TreeNode>(new[] { Root });
			var list = new List<TreeNode>();
			var map = vNodeMap.Keys.ToDictionary(key => key, val => false);

			while ( queue.Count > 0 ) {
				TreeNode n = queue[0];
				queue.RemoveAt(0);

				list.Add(n);
				map[n.SynSet.Id] = true;

				foreach ( TreeNode sub in n.Hyponyms ) {
					if ( !map[sub.SynSet.Id] ) {
						queue.Add(sub);
					}
				}
			}

			return list;
		}

		/*--------------------------------------------------------------------------------------------* /
		private static void WriteHypernymTree() {
			Console.WriteLine(" - Writing HypernymTree file...");

			using ( StreamWriter sw = new StreamWriter(MainClass.DataDir+"HypernymTree.txt") ) {
				sw.Write(Root.ToString(0, 99));
			}
		}

		/*--------------------------------------------------------------------------------------------* /
		private static void WriteSortedHypernymList() {
			Console.WriteLine(" - Writing SortedSynsets file...");
			int wordCount = 0;

			using ( StreamWriter sw = new StreamWriter(MainClass.DataDir+"SortedSynsets.txt") ) {
				foreach ( TreeNode n in sortList ) {
					sw.WriteLine(n.ToString());

					foreach ( Word w in n.SynSet.WordList ) {
						sw.WriteLine(" - "+w.Name);
						wordCount++;
					}
				}
			}

			Console.WriteLine(" * Wrote "+sortList.Count+" Synsets, "+wordCount+" Words");
		}*/

	}

}