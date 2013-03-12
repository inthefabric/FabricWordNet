using Fabric.Apps.WordNet.Data;
using NHibernate;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public class MainClass {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void Main(string[] pArgs) {
			DbBuilder.InitOnce();

			using ( ISession sess = new SessionProvider().OpenSession() ) {
				//BuildWordNet.BuildBaseDb(sess);
				Stats.PrintAll(sess);
			}
		}

	}
	
}