using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class SemanticMap : ClassMap<Semantic> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SemanticMap() {
			Id(x => x.Id)
				.Column(typeof(Semantic).Name+"Id")
				.GeneratedBy.Native();

			References(x => x.Synset);
			Map(x => x.RelationId);
			References(x => x.TargetSynset);

			HasMany(x => x.FactorList); //0 or 1
		}

	}

}