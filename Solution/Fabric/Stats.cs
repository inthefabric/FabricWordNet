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

			foreach ( object[] vals in lexCounts ) {
				Console.WriteLine("Lexical "+Relations[int.Parse(vals[0]+"")]+": "+vals[1]);
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

			foreach ( object[] vals in semCounts ) {
				Console.WriteLine("Semantic "+Relations[int.Parse(vals[0]+"")]+": "+vals[1]);
			}
		}

	}
	
}