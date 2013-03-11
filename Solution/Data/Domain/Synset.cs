using System.Collections.Generic;

namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Synset {

		public virtual int Id { get; protected set; }
		public virtual string SsId { get; set; }
		public virtual byte PartOfSpeechId { get; set; }
		public virtual string Gloss { get; set; }
		public virtual Artifact CreatedArtifact { get; set; }

		public virtual IList<Word> WordList { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Synset() {
			WordList = new List<Word>();
		}

	}

}