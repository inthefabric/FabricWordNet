using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;
using NHibernate;
using NHibernate.Criterion;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public static class Stats {

		public static List<WordNetEngine.SynSetRelation> Relations =
			Enum.GetValues(typeof(WordNetEngine.SynSetRelation))
				.Cast<WordNetEngine.SynSetRelation>().ToList();

		public static List<WordNetEngine.POS> PartsOfSpeech =
			Enum.GetValues(typeof(WordNetEngine.POS))
				.Cast<WordNetEngine.POS>().ToList();


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void PrintAll(ISession pSess) {
			Console.WriteLine();
			PrintLexicalCountsByRel(pSess);
			Console.WriteLine();
			PrintSemanticCountsByRel(pSess);
			Console.WriteLine();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void PrintLexicalCountsByRel(ISession pSess) {
			IList<object[]> lexCounts = pSess.QueryOver<Lexical>()
				.SelectList(list => list
					.Select(x => x.RelationId)
					.SelectCount(x => x.RelationId)
					.SelectGroup(x => x.RelationId)
				)
				.OrderBy(Projections.Count<Lexical>(x => x.RelationId)).Desc
				.List<object[]>();

			Console.WriteLine("Lexical Stats:");

			foreach ( object[] vals in lexCounts ) {
				Console.WriteLine(" - "+Relations[int.Parse(vals[0]+"")]+": "+vals[1]);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		public static void PrintSemanticCountsByRel(ISession pSess) {
			IList<object[]> semCounts = pSess.QueryOver<Semantic>()
				.SelectList(list => list
					.Select(x => x.RelationId)
					.SelectCount(x => x.RelationId)
					.SelectGroup(x => x.RelationId)
				)
				.OrderBy(Projections.Count<Semantic>(x => x.RelationId)).Desc
				.List<object[]>();

			Console.WriteLine("Semantic Stats:");

			foreach ( object[] vals in semCounts ) {
				Console.WriteLine(" - "+Relations[int.Parse(vals[0]+"")]+": "+vals[1]);
			}
		}

		/*--------------------------------------------------------------------------------------------* /
		public static void PrintHypernymMismatches(ISession pSess) {
			IList<Semantic> hyperList = pSess.QueryOver<Semantic>()
				.Where(x => x.RelationId == (byte)WordNetEngine.SynSetRelation.Hypernym)
				.List();

			IList<Semantic> hypoList = pSess.QueryOver<Semantic>()
				.Where(x => x.RelationId == (byte)WordNetEngine.SynSetRelation.Hyponym)
				.List();

			var hyperMap = new HashSet<string>();
			var hypoMap = new HashSet<string>();
			var idMap = new HashSet<string>();

			foreach ( Semantic hyper in hyperList ) {
				hyperMap.Add(hyper.SynSet.Id+","+hyper.TargetSynSet.Id);
			}

			foreach ( Semantic hypo in hypoList ) {
				hypoMap.Add(hypo.TargetSynSet.Id+","+hypo.SynSet.Id);
			}

			IEnumerable<string> result = hyperMap.Except(hypoMap);

			foreach ( string pair in result ) {
				Console.WriteLine("Hyper: "+pair);

				string[] ids = pair.Split(',');
				idMap.Add(ids[0]);
				idMap.Add(ids[1]);
			}

			Console.WriteLine();
			result = hypoMap.Except(hyperMap);

			foreach ( string pair in result ) {
				Console.WriteLine("Hypo: "+pair);

				string[] ids = pair.Split(',');
				idMap.Add(ids[0]);
				idMap.Add(ids[1]);
			}

			Console.WriteLine();

			foreach ( string id in idMap ) {
				Synset ss = pSess.QueryOver<Synset>()
					.Where(x => x.Id == int.Parse(id))
					.Fetch(x => x.WordList).Eager
					.SingleOrDefault();

				string words = ss.WordList.Aggregate("", (x,w) => x+(w.Name+", "));
				Console.WriteLine("Synset "+id+" = "+words);
			}
		}*/

	}
	
}