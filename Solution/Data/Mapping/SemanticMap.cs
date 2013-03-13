using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class SemanticMap : ClassMap<Semantic> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SemanticMap() {
			Id(x => x.Id)
				.Column("SemanticId")
				.GeneratedBy.Native();

			References(x => x.SynSet);
			Map(x => x.RelationId);
			References(x => x.TargetSynSet);

			HasMany(x => x.FactorList); //0 or 1
		}

	}

}