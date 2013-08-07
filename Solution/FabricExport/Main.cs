using Fabric.Apps.WordNet.Data;
using Fabric.Clients.Cs;

namespace Fabric.Apps.WordNet.Export {

	/*================================================================================================*/
	public class MainClass {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void Main(string[] pArgs) {
			DbBuilder.InitOnce();
			DbBuilder.UpdateSchema();
			InitFabricClient();

			var commander = new Commander();
			commander.Start();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void InitFabricClient() {
			const bool LOCAL = true;
			const bool LIVE_TEST = false;
			FabricClientConfig config;

			if ( LOCAL ) {
				config = new FabricClientConfig("WordNetExport", "http://localhost:9000",
					2, "abcdefghijklmnopqrstuvwxyZ012345", 1,
					"http://localhost:55555/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			else if ( LIVE_TEST ) {
				config = new FabricClientConfig("WordNetExport", "http://api.inthefabric.com",
					2, "abcdefghijklmnopqrstuvwxyZ012345", 1,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			else {
				config = new FabricClientConfig("WordNetExport", "http://api.inthefabric.com",
					44630861574832128, "963a27e20ad1472888521ea148a2dcf2", 44630852211048448,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			
			config.Logger = new FabLog();

			FabricClient.InitOnce(config);
		}

	}
	
}