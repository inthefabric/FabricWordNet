using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class WordMap : ClassMap<Word> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public WordMap() {
			Id(x => x.Id)
				.Column(typeof(Word).Name+"Id")
				.GeneratedBy.Native();

			References(x => x.Synset);
			Map(x => x.Name);

			HasMany(x => x.LexicalList);
			HasMany(x => x.LexicalTargetList).KeyColumn("Target"+typeof(Word).Name+"Id");
			HasMany(x => x.ArtifactList); //0 or 1
		}

	}

}