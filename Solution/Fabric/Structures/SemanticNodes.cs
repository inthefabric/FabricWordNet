using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
			//WriteSortedHypernymList();
		}

		/*--------------------------------------------------------------------------------------------*/
		private void BuildMaps(ISession pSess) {
			/*long t = DateTime.UtcNow.Ticks;
			Console.WriteLine(" - Getting all Semantics...");
			IList<Semantic> semList = pSess.QueryOver<Semantic>().List();
			Console.WriteLine(" - Found "+semList.Count+" Semantics");
			Console.WriteLine(" *** Time: "+(DateTime.UtcNow.Ticks-t)/10000000.0+" sec");

			Console.WriteLine(" - Getting all Lexicals...");
			IList<Lexical> lexList = pSess.QueryOver<Lexical>().List();
			Console.WriteLine(" - Found "+lexList.Count+" Lexicals");
			Console.WriteLine(" *** Time: "+(DateTime.UtcNow.Ticks-t)/10000000.0+" sec");

			Console.WriteLine(" - Getting all Synsets, filled with Words...");
			IList<Synset> synList = pSess.QueryOver<Synset>()
				.Fetch(x => x.WordList).Eager
				.TransformUsing(Transformers.DistinctRootEntity)
				.List();
			Console.WriteLine(" - Found "+synList.Count+" Synsets");
			Console.WriteLine(" *** Time: "+(DateTime.UtcNow.Ticks-t)/10000000.0+" sec");*/

			long t = DateTime.UtcNow.Ticks;
			IList<Semantic> semList = null;
			IList<Lexical> lexList = null;
			IList<Synset> synList = null;
			
			Parallel.Invoke(
				() => {
					Console.WriteLine(" - Getting all Semantics...");
					using ( ISession innerSess = pSess.SessionFactory.OpenSession() ) {
						semList = innerSess.QueryOver<Semantic>().List();
					}
					Console.WriteLine(" - Found "+semList.Count+" Semantics");
					Console.WriteLine(" *** Time: "+(DateTime.UtcNow.Ticks-t)/10000000.0+" sec");
				},
				() => {
					Console.WriteLine(" - Getting all Lexicals...");
					using ( ISession innerSess = pSess.SessionFactory.OpenSession() ) {
						lexList = innerSess.QueryOver<Lexical>().List();
					}
					Console.WriteLine(" - Found "+lexList.Count+" Lexicals");
					Console.WriteLine(" *** Time: "+(DateTime.UtcNow.Ticks-t)/10000000.0+" sec");
				},
				() => {
					Console.WriteLine(" - Getting all Synsets, filled with Words...");
					using ( ISession innerSess = pSess.SessionFactory.OpenSession() ) {
						synList = innerSess.QueryOver<Synset>()
							.Fetch(x => x.WordList).Eager
							.TransformUsing(Transformers.DistinctRootEntity)
							.List();
					}
					Console.WriteLine(" - Found "+synList.Count+" Synsets");
					Console.WriteLine(" *** Time: "+(DateTime.UtcNow.Ticks-t)/10000000.0+" sec");
				}
			);

			Console.WriteLine(" *** Time: "+(DateTime.UtcNow.Ticks-t)/10000000.0+" sec");

			Console.WriteLine(" - Building maps...");
			SynMap = synList.ToDictionary(x => x.Id);
			NodeMap = new Dictionary<int, SemanticNode>();

			foreach ( Synset ss in synList ) {
				NodeMap[ss.Id] = new SemanticNode(ss);
			}

			foreach ( Semantic sem in semList ) {
				SemanticNode n = NodeMap[sem.Synset.Id];
				n.AddRelation(Stats.Relations[sem.RelationId], NodeMap[sem.TargetSynset.Id]);
			}

			foreach ( Lexical lex in lexList ) {
				SemanticNode n = NodeMap[lex.Synset.Id];
				n.AddLexical(Stats.Relations[lex.RelationId], NodeMap[lex.TargetSynset.Id]);
			}

			pSess.Clear();
		}

		/*--------------------------------------------------------------------------------------------*/
		private void BuildSortedNodeList() {
			Console.WriteLine(" - Building sorted SemanticNode list...");

			NodeList = new List<SemanticNode>();
			var queue = new List<SemanticNode>(new[] { EntityNode });
			var map = NodeMap.Keys.ToDictionary(key => key, val => false);
			map[queue[0].SynSet.Id] = true;

			while ( queue.Count > 0 ) {
				SemanticNode n = queue[0];
				queue.RemoveAt(0);
				NodeList.Add(n);

				foreach ( SemanticNode sub in n.Relations[WordNetEngine.SynSetRelation.Hyponym] ) {
					if ( !map[sub.SynSet.Id] ) {
						map[sub.SynSet.Id] = true;
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