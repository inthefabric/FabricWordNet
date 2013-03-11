namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Lexical {

		public virtual int Id { get; protected set; }
		public virtual Synset SynSet { get; set; }
		public virtual byte RelationId { get; set; }
		public virtual string Word { get; set; }
		public virtual string RelatedWord { get; set; }
		public virtual Factor CreatedFactor { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Lexical() {

		}

	}

}