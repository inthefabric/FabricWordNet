﻿namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Factor {

		public virtual int Id { get; protected set; }
		public virtual Artifact PrimaryClass { get; set; }
		public virtual Artifact RelatedClass { get; set; }
		public virtual byte AssertionId { get; set; }
		public virtual bool IsDefining { get; set; }
		public virtual string Note { get; set; }
		public virtual long ActualFactorId { get; set; }

		public virtual byte DescriptorTypeId { get; set; }
		public virtual Artifact DescriptorTypeRefine { get; set; }
		public virtual Artifact PrimaryClassRefine { get; set; }
		public virtual Artifact RelatedClassRefine { get; set; }
		public virtual long ActualDescriptorId { get; set; }

		public virtual byte IdentorTypeId { get; set; }
		public virtual string IdentorValue { get; set; }
		public virtual long ActualIdentorId { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Factor() {

		}

	}

}