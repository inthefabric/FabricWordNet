using System;
using System.Collections.Generic;
using System.Threading;
using Fabric.Clients.Cs.Session;

namespace Fabric.Apps.WordNet.Export {

	/*================================================================================================*/
	public class FabSess : FabricSessionContainer {//, IContextProperty {

		private static readonly Dictionary<int, FabSess> ThreadSessions = InitSessions();

		//public string Name { get; private set;  }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------* /
		public FabSess(string pName) {
			Name = pName;
		}
		
		/*--------------------------------------------------------------------------------------------* /
		public bool IsNewContextOK(Context pNewCtx) {
			return true;
		}

		/*--------------------------------------------------------------------------------------------* /
		public void Freeze(Context pNewCtx) {}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static Dictionary<int, FabSess> InitSessions() {
			return new Dictionary<int, FabSess>();
		}

		/*--------------------------------------------------------------------------------------------*/
		public static IFabricSessionContainer FabricSessionContainerProvider(string pConfigKey) {
			/*string name = "Fabric_"+pConfigKey;
			Context ctx = Thread.CurrentContext;
			Console.WriteLine(ctx.ContextID+" / "+Thread.CurrentThread.IsAlive+" / "+
				Thread.CurrentThread.Name+" / "+Thread.CurrentThread);

			FabSess sess = (FabSess)ctx.GetProperty(name);

			if ( sess == null ) {
				sess = new FabSess(name);
				ctx.SetProperty(sess);
			}*/

			FabSess sess;
			int id = Thread.CurrentThread.ManagedThreadId;
			ThreadSessions.TryGetValue(id, out sess);
			
			//Console.WriteLine("## "+id+" / "+Thread.CurrentThread.IsAlive+" / "+
			//	Thread.CurrentThread.Name+" / "+sess);

			if ( sess == null ) {
				sess = new FabSess();
				ThreadSessions.Add(id, sess);
			}

			return sess;
		}

	}
	
}