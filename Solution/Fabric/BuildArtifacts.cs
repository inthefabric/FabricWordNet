using Fabric.Apps.WordNet.Artifacts;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public static class BuildArtifacts {

		//SELECT Name, Disamb, COUNT(Name) FROM Artifact GROUP BY Name, Disamb HAVING COUNT(Name) > 1 ORDER BY COUNT(Name) DESC


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void InsertWordAndSynsetArtifacts(ISession pSess) {
			pSess.CreateSQLQuery("DELETE FROM "+typeof(Artifact).Name+" WHERE 1=1").UniqueResult();
			BuildWordNet.SetDbStateBeforeBatchInsert(pSess);

			var nodes = new SemanticNodes(pSess);
			
			var iaa = new InsertAllArtifacts(nodes);
			iaa.Insert(pSess);

			BuildWordNet.SetDbStateAfterBatchInsert(pSess);
		}

	}

}