using System.Collections.Generic;

namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Word {

		public virtual int Id { get; protected set; }
		public virtual Synset Synset { get; set; }
		public virtual string Name { get; set; }

		public virtual IList<Lexical> LexicalList { get; set; }
		public virtual IList<Lexical> TargetLexicalList { get; set; }
		public virtual IList<Artifact> ArtifactList { get; set; } //0 or 1


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Word() {
			LexicalList = new List<Lexical>();
			TargetLexicalList = new List<Lexical>();
			ArtifactList = new List<Artifact>();
		}

	}

}