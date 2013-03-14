using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class SynsetMap : ClassMap<Synset> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SynsetMap() {
			Id(x => x.Id)
				.Column(typeof(Synset).Name+"Id")
				.GeneratedBy.Native();

			Map(x => x.SsId);
			Map(x => x.PartOfSpeechId);
			Map(x => x.Gloss);

			HasMany(x => x.WordList);
			HasMany(x => x.SemanticList);
			HasMany(x => x.SemanticTargetList).KeyColumn("Target"+typeof(Synset).Name+"Id");
			HasMany(x => x.LexicalList);
			HasMany(x => x.LexicalTargetList).KeyColumn("Target"+typeof(Synset).Name+"Id");
			HasMany(x => x.ArtifactList); //0 or 1
		}

	}

}