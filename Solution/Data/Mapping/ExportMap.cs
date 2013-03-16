using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class ExportMap : ClassMap<Export> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ExportMap() {
			Id(x => x.Id)
				.Column(typeof(Export).Name+"Id")
				.GeneratedBy.Native();

			Map(x => x.FabricId);
			References(x => x.Artifact).Nullable();
			References(x => x.Factor).Nullable();

			Map(x => x.Timestamp);
			Map(x => x.DataLen);
			Map(x => x.DbMs);
			Map(x => x.TotalMs);
		}

	}

}