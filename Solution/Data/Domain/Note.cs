namespace Fabric.Apps.WordNet.Data.Domain {

	/*================================================================================================*/
	public enum NoteType {
		SynsetMeansGloss = 1,
		WordMeansSynset
	}


	/*================================================================================================*/
	public class Note {

		public virtual int Id { get; protected set; }
		public virtual byte Type { get; set; }
		public virtual string Text { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static Note New(NoteType pType, string pText) {
			var note = new Note();
			note.Type = (byte)pType;
			note.Text = pText;
			return note;
		}

	}

}
