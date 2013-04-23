using System.Collections.Generic;

namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Job {

		public virtual int Id { get; protected set; }
		public virtual long TimeStart { get; set; }
		public virtual long TimeEnd { get; set; }

		public virtual IList<Batch> BatchList { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Job() {
			BatchList = new List<Batch>();
		}

	}

}