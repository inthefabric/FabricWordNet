﻿namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Semantic {

		public virtual int Id { get; protected set; }
		public virtual Synset SynSet { get; set; }
		public virtual byte RelationId { get; set; }
		public virtual Synset TargetSynSet { get; set; }
		public virtual Factor CreatedFactor { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Semantic() {

		}

	}

}