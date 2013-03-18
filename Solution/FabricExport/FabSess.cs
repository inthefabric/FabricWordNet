using System;
using System.Collections.Generic;
using System.Threading;
using Fabric.Clients.Cs.Session;

namespace Fabric.Apps.WordNet.Export {

	/*================================================================================================*/
	public class FabSess : FabricSessionContainer {

		private static readonly Dictionary<int, FabSess> ThreadSessions = InitSessions();


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static Dictionary<int, FabSess> InitSessions() {
			return new Dictionary<int, FabSess>();
		}

		/*--------------------------------------------------------------------------------------------*/
		public static IFabricSessionContainer FabricSessionContainerProvider(string pConfigKey) {
			FabSess sess;
			int id = Thread.CurrentThread.ManagedThreadId;
			ThreadSessions.TryGetValue(id, out sess);

			if ( sess == null ) {
				sess = new FabSess();
				ThreadSessions.Add(id, sess);
			}

			return sess;
		}

	}
	
}