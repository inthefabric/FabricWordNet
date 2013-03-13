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
		public virtual long ActualArtifactId { get; set; }

		public virtual IList<Factor> FactorList { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Artifact() {
			FactorList = new List<Factor>();
		}

	}

}