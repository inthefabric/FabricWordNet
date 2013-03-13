using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class SynsetMap : ClassMap<Synset> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SynsetMap() {
			Id(x => x.Id)
				.Column("SynsetId")
				.GeneratedBy.Native();

			Map(x => x.SsId);
			Map(x => x.PartOfSpeechId);
			Map(x => x.Gloss);

			HasMany(x => x.WordList);
			HasMany(x => x.ArtifactList); //0 or 1
		}

	}

}