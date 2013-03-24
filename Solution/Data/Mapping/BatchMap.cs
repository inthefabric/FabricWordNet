using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class BatchMap : ClassMap<Batch> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public BatchMap() {
			Id(x => x.Id)
				.Column(typeof(Batch).Name+"Id")
				.GeneratedBy.Native();

			References(x => x.Job);
			Map(x => x.Size);
			Map(x => x.Count);
			Map(x => x.Threads);
			Map(x => x.Timestamp);
			Map(x => x.DataLen);
			Map(x => x.DbMs);
			Map(x => x.TotalMs);

			HasMany(x => x.ExportList);
		}

	}

}