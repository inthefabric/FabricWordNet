using System.Collections.Generic;

namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Artifact {

		public virtual int Id { get; protected set; }
		public virtual string Name { get; set; }
		public virtual string Disamb { get; set; }
		public virtual string Note { get; set; }

		public virtual Synset Synset { get; set; }
		public virtual Word Word { get; set; }

		public virtual IList<Factor> FactorPrimaryList { get; set; }
		public virtual IList<Factor> FactorRelatedList { get; set; }
		public virtual IList<Factor> FactorDescTypeList { get; set; }
		public virtual IList<Factor> FactorDescPrimaryList { get; set; }
		public virtual IList<Factor> FactorDescRelatedList { get; set; }
		public virtual IList<Export> ExportList { get; set; } //0 or 1


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Artifact() {
			FactorPrimaryList = new List<Factor>();
			FactorRelatedList = new List<Factor>();
			FactorDescTypeList = new List<Factor>();
			FactorDescPrimaryList = new List<Factor>();
			FactorDescRelatedList = new List<Factor>();
			ExportList = new List<Export>();
		}

	}

}