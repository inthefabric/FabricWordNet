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
			const bool LOCAL = false;
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
					40949086160945152, "847e1a71c60146acaa4f4d0fbd6fd180", 40949076406042624,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			
			config.Logger = new FabLog();

			FabricClient.InitOnce(config);
		}

	}
	
}