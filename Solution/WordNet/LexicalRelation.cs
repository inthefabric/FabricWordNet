namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public class LexicalRelation {

		public WordNetEngine.SynSetRelation Relation { get; set; }
		public SynSet FromSyn { get; set; }
		public string FromWord { get; set; }
		public SynSet ToSyn { get; set; }
		public string ToWord { get; set; }

	}

}