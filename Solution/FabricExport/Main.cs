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
			FabricClientConfig config;

			if ( LOCAL ) {
				config = new FabricClientConfig("WordNetExport", "http://localhost:9000",
					6, "0123456789abcdefghijkLMNOPqrstuv", 5,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			else {
				config = new FabricClientConfig("WordNetExport", "http://api.inthefabric.com",
					42927616976486400, "a30aed036ec54c64a651b80a29a2f951", 42927604513112064,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			
			config.Logger = new FabLog();

			FabricClient.InitOnce(config);
		}

	}
	
}