using NHibernate;

namespace Fabric.Apps.WordNet.Data {

	/*================================================================================================*/
	public class SessionProvider : ISessionProvider {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ISession OpenSession() {
			return DbBuilder.SessionFactory.OpenSession();
		}

	}

}