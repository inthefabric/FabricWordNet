using System;
using System.IO;
using Fabric.Apps.WordNet.Artifacts;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Factors;
using Fabric.Apps.WordNet.Notes;
using Fabric.Apps.WordNet.Wordnet;
using NHibernate;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public class MainClass {

		public static readonly string DataDir = Directory.GetCurrentDirectory()+"/../../../../Data/";


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void Main(string[] pArgs) {
			DbBuilder.InitOnce();
			//DbBuilder.UpdateSchema();
			const int step = -1;

			NotePrep.Process();
			NoteWrite.WriteAll();

			Console.Write("Press any key to close...");
			Console.ReadKey();
			return;

			switch ( step ) {
				case 0:
					using ( ISession sess = new SessionProvider().OpenSession() ) {
						BuildWordNet.BuildBaseDb(sess);
						//Stats.PrintAll(sess);
					}
					break;

				case 1:
					using ( ISession sess = new SessionProvider().OpenSession() ) {
						BuildArtifacts.InsertWordAndSynsetArtifacts(sess);
					}
					break;

				case 2:
					BuildFactors.InsertAllFactors();
					break;

			}
		}
	}
	
}