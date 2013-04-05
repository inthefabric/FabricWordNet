using System.Collections.Generic;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using Fabric.Apps.WordNet.Wordnet;
using NHibernate;

namespace Fabric.Apps.WordNet.Factors {

	/*================================================================================================*/
	public static class BuildFactors {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void InsertAllFactors() {
			var sessProv = new SessionProvider();

			var descMap = new Dictionary<WordNetEngine.SynSetRelation, DescriptorTypeId>();
			descMap.Add(WordNetEngine.SynSetRelation.Hypernym, DescriptorTypeId.IsA);

			using ( ISession sess = sessProv.OpenSession() ) {
				sess.CreateSQLQuery("DELETE FROM "+typeof(Factor).Name).UniqueResult();
				BuildWordNet.SetDbStateBeforeBatchInsert(sess);
			}

			ArtifactSet artSet;

			using ( ISession sess = sessProv.OpenSession() ) {
				artSet = new ArtifactSet(sess);
			}

			var sf = new SemanticFactors(artSet);
			sf.Start();

			var lf = new LexicalFactors(artSet);
			lf.Start();

			using ( ISession sess = sessProv.OpenSession() ) {
				BuildWordNet.SetDbStateAfterBatchInsert(sess);
			}
		}

	}

}