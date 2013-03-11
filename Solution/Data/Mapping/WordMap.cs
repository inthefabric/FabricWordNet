using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class WordMap : ClassMap<Word> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public WordMap() {
			Id(x => x.Id)
				.Column("WordId")
				.GeneratedBy.Native();

			References(x => x.SynSet);
			Map(x => x.Name);
			References(x => x.CreatedArtifact).Nullable();
		}

	}

}