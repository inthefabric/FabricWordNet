using System.Collections.Generic;

namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Batch {

		public virtual int Id { get; protected set; }
		public virtual Job Job { get; set; }
		public virtual int Size { get; set; }
		public virtual int Count { get; set; }
		public virtual int Threads { get; set; }
		public virtual long Timestamp { get; set; }
		public virtual int DataLen { get; set; }
		public virtual int DbMs { get; set; }
		public virtual int TotalMs { get; set; }

		public virtual IList<Export> ExportList { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Batch() {
			ExportList = new List<Export>();
		}

	}

}