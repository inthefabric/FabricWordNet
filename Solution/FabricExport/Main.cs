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
			var config = new FabricClientConfig("ExportTest", "http://localhost:9000", 2,
				"0123456789abcdefghijkLMNOPqrstuv", 4, "http://localhost:49316/OAuth/FabricRedirect",
				FabSess.FabricSessionContainerProvider);
			config.Logger = new FabLog();

			FabricClient.InitOnce(config);
		}

	}
	
}