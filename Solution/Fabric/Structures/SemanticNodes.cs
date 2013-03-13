using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;
using NHibernate;
using NHibernate.Transform;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class SemanticNodes {

		public Dictionary<int, Synset> SynMap { get; private set; }
		public Dictionary<int, SemanticNode> NodeMap { get; private set; }
		public SemanticNode EntityNode { get; private set; }
		public List<SemanticNode> NodeList { get; private set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SemanticNodes(ISession pSess) {
			Console.WriteLine("SEMANTIC NODES");
			BuildMaps(pSess);
			EntityNode = NodeMap[59724];
			BuildSortedNodeList();
			WriteSortedHypernymList();
		}

		/*--------------------------------------------------------------------------------------------*/
		private void BuildMaps(ISession pSess) {
			Console.WriteLine(" - Getting all Semantics...");
			IList<Semantic> semList = pSess.QueryOver<Semantic>().List();
			Console.WriteLine(" - Found "+semList.Count+" Semantics");

			Console.WriteLine(" - Getting all Synsets, filled with Words...");
			IList<Synset> synList = pSess.QueryOver<Synset>()
				.Fetch(x => x.WordList).Eager
				.TransformUsing(Transformers.DistinctRootEntity)
				.List();
			Console.WriteLine(" - Found "+synList.Count+" Synsets");

			Console.WriteLine(" - Building maps...");
			SynMap = synList.ToDictionary(x => x.Id);
			NodeMap = new Dictionary<int, SemanticNode>();

			foreach ( Synset ss in synList ) {
				NodeMap[ss.Id] = new SemanticNode(ss);
			}

			foreach ( Semantic sem in semList ) {
				SemanticNode n = NodeMap[sem.SynSet.Id];
				n.AddRelation(Stats.Relations[sem.RelationId], NodeMap[sem.TargetSynSet.Id]);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void BuildSortedNodeList() {
			Console.WriteLine(" - Building sorted SemanticNode list...");

			NodeList = new List<SemanticNode>();
			var queue = new List<SemanticNode>(new[] { EntityNode });
			var map = NodeMap.Keys.ToDictionary(key => key, val => false);

			while ( queue.Count > 0 ) {
				SemanticNode n = queue[0];
				queue.RemoveAt(0);

				NodeList.Add(n);
				map[n.SynSet.Id] = true;

				foreach ( SemanticNode sub in n.Relations[WordNetEngine.SynSetRelation.Hyponym] ) {
					if ( !map[sub.SynSet.Id] ) {
						queue.Add(sub);
					}
				}
			}

			foreach ( KeyValuePair<int, bool> pair in map ) {
				if ( !pair.Value ) {
					NodeList.Add(NodeMap[pair.Key]);
				}
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void WriteSortedHypernymList() {
			Console.WriteLine(" - Writing SortedSemanticNodes file...");
			int wordCount = 0;

			using ( StreamWriter sw = new StreamWriter(MainClass.DataDir+"SortedSemanticNodes.txt") ) {
				foreach ( SemanticNode n in NodeList ) {
					sw.WriteLine(n.ToString());

					foreach ( Word w in n.SynSet.WordList ) {
						sw.WriteLine(" - "+w.Name);
						wordCount++;
					}
				}
			}

			Console.WriteLine(" - Wrote "+NodeList.Count+" Synsets, "+wordCount+" Words");
		}

	}

}