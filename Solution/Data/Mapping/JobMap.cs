using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class JobMap : ClassMap<Job> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public JobMap() {
			Id(x => x.Id)
				.Column(typeof(Job).Name+"Id")
				.GeneratedBy.Native();

			Map(x => x.Size);
			Map(x => x.Count);
			Map(x => x.Threads);
			Map(x => x.TimeStart);
			Map(x => x.TimeEnd);

			HasMany(x => x.BatchList);
		}

	}

}