using System.Collections.Generic;

namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Lexical {

		public virtual int Id { get; protected set; }
		public virtual Synset Synset { get; set; } //TOOD: remove 
		public virtual Word Word { get; set; }
		public virtual byte RelationId { get; set; }
		public virtual Synset TargetSynset { get; set; } //TOOD: remove 
		public virtual Word TargetWord { get; set; }

		public virtual IList<Factor> FactorList { get; set; } //0 or 1


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Lexical() {

		}

	}

}