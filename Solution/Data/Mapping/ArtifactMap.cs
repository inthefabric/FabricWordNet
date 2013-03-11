using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class ArtifactMap : ClassMap<Artifact> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ArtifactMap() {
			Id(x => x.Id)
				.Column("ArtifactId")
				.GeneratedBy.Native();

			Map(x => x.Name);
            Map(x => x.Disamb);
            Map(x => x.Note);
			Map(x => x.ActualArtifactId).Nullable();

			HasMany(x => x.FactorList);
		}

	}

}