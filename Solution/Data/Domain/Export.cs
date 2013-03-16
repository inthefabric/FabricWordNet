namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Export {

		public virtual int Id { get; protected set; }
		public virtual long FabricId { get; set; }
		public virtual Artifact Artifact { get; set; }
		public virtual Factor Factor { get; set; }

		public virtual long Timestamp { get; set; }
		public virtual int DataLen { get; set; }
		public virtual int DbMs { get; set; }
		public virtual int TotalMs { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Export() {
		}

	}

}