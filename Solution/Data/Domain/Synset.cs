using System.Collections.Generic;

namespace Fabric.Apps.WordNet.Data.Domain {
	
	/*================================================================================================*/
	public class Synset {

		public virtual int Id { get; protected set; }
		public virtual string SsId { get; set; }
		public virtual byte PartOfSpeechId { get; set; }
		public virtual string Gloss { get; set; }

		public virtual IList<Word> WordList { get; set; }
		public virtual IList<Semantic> SemanticList { get; set; }
		public virtual IList<Semantic> SemanticTargetList { get; set; }
		public virtual IList<Lexical> LexicalList { get; set; }
		public virtual IList<Lexical> LexicalTargetList { get; set; }
		public virtual IList<Artifact> ArtifactList { get; set; } //0 or 1

		public virtual int SortValue { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Synset() {
			WordList = new List<Word>();
			SemanticList = new List<Semantic>();
			SemanticTargetList = new List<Semantic>();
			LexicalList = new List<Lexical>();
			LexicalTargetList = new List<Lexical>();
			ArtifactList = new List<Artifact>();
		}

	}

}