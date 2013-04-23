using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class LexicalMap : ClassMap<Lexical> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public LexicalMap() {
			Id(x => x.Id)
				.Column(typeof(Lexical).Name+"Id")
				.GeneratedBy.Native();

			References(x => x.Synset);
			References(x => x.Word);
			Map(x => x.RelationId);
			References(x => x.TargetSynset);
			References(x => x.TargetWord);

			HasMany(x => x.FactorList); //0 or 1
		}

	}

}