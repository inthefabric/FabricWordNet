namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Word {

		public virtual int Id { get; protected set; }
		public virtual Synset SynSet { get; set; }
		public virtual string Name { get; set; }
		public virtual Artifact CreatedArtifact { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Word() {

		}

	}

}