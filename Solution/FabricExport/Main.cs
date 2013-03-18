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
			var config = new FabricClientConfig("WordNetExport", "http://api.inthefabric.com",
				159747403417649152, "4e0037f6ce2f4028b4d69646bd24df3d", 159747300745281536, 
				"http://localhost:49316/OAuth/FabricRedirect",
				FabSess.FabricSessionContainerProvider);
			config.Logger = new FabLog();

			FabricClient.InitOnce(config);
		}

	}
	
}