using System;
using NHibernate;
using NHibernate.SqlCommand;

namespace Fabric.Apps.WordNet.Data {

	/*================================================================================================*/
	public class SessionProvider : EmptyInterceptor {

		public bool OutputSql { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ISession OpenSession() {
			return DbBuilder.SessionFactory.OpenSession(this);
		}

		/*--------------------------------------------------------------------------------------------*/
		public override SqlString OnPrepareStatement(SqlString pSql) {
			if ( OutputSql ) {
				Console.WriteLine("...... "+pSql);
			}

			return base.OnPrepareStatement(pSql);
		}

	}

}