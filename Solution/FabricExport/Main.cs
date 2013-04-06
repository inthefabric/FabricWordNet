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
			FabricClientConfig config;

			if ( LOCAL ) {
				config = new FabricClientConfig("WordNetExport", "http://localhost:9000",
					2, "0123456789abcdefghijkLMNOPqrstuv", 4,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			else {
				config = new FabricClientConfig("WordNetExport", "http://api.inthefabric.com",
					41726164196130816, "e15569e20ffc45e3af0d1de1c4dc8b14", 41726154925670400,
					"http://localhost:49316/OAuth/FabricRedirect",
					FabSess.FabricSessionContainerProvider);
			}
			
			config.Logger = new FabLog();

			FabricClient.InitOnce(config);
		}

	}
	
}