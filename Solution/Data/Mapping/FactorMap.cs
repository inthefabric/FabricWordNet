using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class FactorMap : ClassMap<Factor> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public FactorMap() {
			Id(x => x.Id)
				.Column("FactorId")
				.GeneratedBy.Native();

			References(x => x.PrimaryClass);
			References(x => x.RelatedClass);
			Map(x => x.AssertionId);
            Map(x => x.IsDefining);
            Map(x => x.Note);

			References(x => x.Lexical).Nullable();
			References(x => x.Semantic).Nullable();
			Map(x => x.ActualFactorId).Nullable();

			Map(x => x.DescriptorTypeId).Nullable();
			References(x => x.DescriptorTypeRefine).Nullable();
			References(x => x.PrimaryClassRefine).Nullable();
			References(x => x.RelatedClassRefine).Nullable();
			Map(x => x.ActualDescriptorId).Nullable();

			Map(x => x.IdentorTypeId).Nullable();
			Map(x => x.IdentorValue).Nullable();
			Map(x => x.ActualIdentorId).Nullable();
			
			Map(x => x.DirectorTypeId).Nullable();
			Map(x => x.PrimaryDirectorActionId).Nullable();
			Map(x => x.RelatedDirectorActionId).Nullable();
			Map(x => x.ActualDirectorId).Nullable();
		}

	}

}