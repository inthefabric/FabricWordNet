using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;
using NHibernate;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class ArtifactSet {

		public IList<Artifact> List { get; private set; }
		public IList<Word> WordList { get; private set; }
		public IDictionary<int, Artifact> IdMap { get; private set; }
		public IDictionary<int, Artifact> SynsetIdMap { get; private set; }
		public IDictionary<int, Artifact> WordIdMap { get; private set; }
		public IDictionary<int, int> WordIdToSynsetIdMap { get; private set; }
		private readonly DateTime vStartTime;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ArtifactSet(ISession pSess) {
			vStartTime = DateTime.UtcNow;
			Console.WriteLine("ARTIFACT SET");

			Console.WriteLine(" - Getting all Artifacts...");
			List = pSess.QueryOver<Artifact>().List();
			Console.WriteLine(" - Found "+List.Count+" Artifacts");
			
			Console.WriteLine(" - Getting all Words...");
			WordList = pSess.QueryOver<Word>().List();
			Console.WriteLine(" - Found "+List.Count+" Artifacts");

			PrintTimer();

			Console.WriteLine(" - Building maps...");
			IdMap = List.ToDictionary(x => x.Id);
			SynsetIdMap = List.Where(x => x.Synset != null).ToDictionary(x => x.Synset.Id);
			WordIdMap = List.Where(x => x.Word != null).ToDictionary(x => x.Word.Id);
			WordIdToSynsetIdMap = WordList.ToDictionary(x => x.Id, x => x.Synset.Id);
			Console.WriteLine(" - Finsihed maps");
			PrintTimer();

			pSess.Clear();
		}

		/*--------------------------------------------------------------------------------------------*/
		private void PrintTimer() {
			Console.WriteLine(" *** ArtifactSet Time: "+(DateTime.UtcNow-vStartTime).TotalSeconds);
		}

	}

}