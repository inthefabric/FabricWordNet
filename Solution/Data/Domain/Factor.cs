using System.Collections.Generic;

namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Factor : IHasNote {

		public virtual int Id { get; protected set; }
		public virtual Artifact PrimaryClass { get; set; }
		public virtual Artifact RelatedClass { get; set; }
		public virtual byte AssertionId { get; set; }
		public virtual bool IsDefining { get; set; }
		public virtual string Note { get; set; }

		public virtual Lexical Lexical { get; set; }
		public virtual Semantic Semantic { get; set; }

		public virtual byte DescriptorTypeId { get; set; }
		public virtual Artifact DescriptorTypeRefine { get; set; }
		public virtual Artifact PrimaryClassRefine { get; set; }
		public virtual Artifact RelatedClassRefine { get; set; }

		public virtual byte IdentorTypeId { get; set; }
		public virtual string IdentorValue { get; set; }

		public virtual IList<Export> ExportList { get; set; } //0 or 1


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Factor() {
			ExportList = new List<Export>();
		}

	}

}