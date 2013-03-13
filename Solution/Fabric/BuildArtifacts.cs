using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public static class BuildArtifacts {

		//SELECT Name, Disamb, COUNT(Name) FROM Artifact GROUP BY Name, Disamb HAVING COUNT(Name) > 1 ORDER BY COUNT(Name) DESC


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void InsertWordAndSynsetArtifacts(ISession pSess) {
			//BuildWordNet.SetDbStateBeforeBatchInsert(pSess);
			//pSess.CreateSQLQuery("DELETE FROM "+typeof(Artifact).Name+" WHERE 1=1").UniqueResult();
			//pSess.CreateSQLQuery("VACUUM").UniqueResult();
			//var tree = new HypernymTree(pSess);
			var nodes = new SemanticNodes(pSess);
			//var ha = new HypernymArtifacts(tree, pSess);
			//var ra = new RemainingArtifacts(tree, pSess);
			//BuildWordNet.SetDbStateAfterBatchInsert(pSess);
		}

	}

}