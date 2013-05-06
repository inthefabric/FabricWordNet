using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class ArtifactMap : ClassMap<Artifact> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ArtifactMap() {
			Id(x => x.Id)
				.Column(typeof(Artifact).Name+"Id")
				.GeneratedBy.Native();

			Map(x => x.Name);
            Map(x => x.Disamb);
            Map(x => x.Note);

			References(x => x.Synset).Nullable();
			References(x => x.Word).Nullable();

			HasMany(x => x.FactorPrimaryList).KeyColumn("PrimaryArtifactId");
			HasMany(x => x.FactorRelatedList).KeyColumn("RelatedArtifactId");
			HasMany(x => x.FactorDescTypeList).KeyColumn("DescriptorTypeRefineId");
			HasMany(x => x.FactorDescPrimaryList).KeyColumn("PrimaryClassRefineId");
			HasMany(x => x.FactorDescRelatedList).KeyColumn("RelatedClassRefineId");
			HasMany(x => x.ExportList);
		}

	}

}