using System.IO;
using Fabric.Apps.WordNet.Artifacts;
using Fabric.Apps.WordNet.Data;
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
			var sessProv = new SessionProvider();

			using ( ISession sess = sessProv.OpenSession() ) {
				BuildWordNet.BuildBaseDb(sess);
				Stats.PrintAll(sess);
				//sessProv.OutputSql = true;
				BuildArtifacts.InsertWordAndSynsetArtifacts(sess);
			}
		}
	}
	
}