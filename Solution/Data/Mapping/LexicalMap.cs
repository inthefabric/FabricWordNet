using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class LexicalMap : ClassMap<Lexical> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public LexicalMap() {
			Id(x => x.Id)
				.Column("LexicalId")
				.GeneratedBy.Native();

			References(x => x.SynSet);
			Map(x => x.RelationId);
			Map(x => x.Word);
			Map(x => x.RelatedWord);

			HasMany(x => x.FactorList); //0 or 1
		}

	}

}