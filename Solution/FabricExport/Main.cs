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
			const bool BYFAB = false;
			FabricClientConfig config;

			if ( LOCAL ) {
				config = new FabricClientConfig("WordNetExport", "http://localhost:9000",
					2, "0123456789abcdefghijkLMNOPqrstuv", 4,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			else if ( BYFAB ) {
				config = new FabricClientConfig("WordNetExport", "http://api.inthefabric.com",
					1, "abcdefghijklmnopqrstuvwxyZ012345", 1,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			else {
				config = new FabricClientConfig("WordNetExport", "http://api.inthefabric.com",
					40779472528474112, "b0dee7dff1484f1ca20a67c9cd949c6f", 40779451267547136,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			
			config.Logger = new FabLog();

			FabricClient.InitOnce(config);
		}

	}
	
}