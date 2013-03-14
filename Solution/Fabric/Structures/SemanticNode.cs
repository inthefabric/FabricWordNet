using System.Collections.Generic;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class SemanticNode {

		public Synset SynSet { get; private set; }
		public IDictionary<WordNetEngine.SynSetRelation, List<SemanticNode>>
																		Relations { get; private set; }
		public IDictionary<string, SemanticNode> RelationMap { get; private set; }
		

		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SemanticNode(Synset pSynSet) {
			SynSet = pSynSet;
			Relations = new Dictionary<WordNetEngine.SynSetRelation, List<SemanticNode>>();
			RelationMap = new Dictionary<string, SemanticNode>();

			foreach ( WordNetEngine.SynSetRelation ssr in Stats.Relations ) {
				Relations.Add(ssr, new List<SemanticNode>());
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public void AddRelation(WordNetEngine.SynSetRelation pRel, SemanticNode pTargNode) {
			Relations[pRel].Add(pTargNode);
			RelationMap.Add(((int)pRel)+"|"+pTargNode.SynSet.Id, pTargNode);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public IDictionary<string, SemanticNode> GetRelationDiff(SemanticNode pCompareNode) {
			return GetRelationDiff(pCompareNode.RelationMap);
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public IDictionary<string, SemanticNode> GetRelationDiff(IDictionary<string, SemanticNode> pCompareMap) {
			return RelationMap
				.Except(pCompareMap)
				.ToDictionary(x => x.Key, x => x.Value);
		}

		/*--------------------------------------------------------------------------------------------*/
		public static IDictionary<string, SemanticNode> GetRelationUnion(
																	IList<SemanticNode> pCompareNodes) {
			IEnumerable<KeyValuePair<string,SemanticNode>> map = new Dictionary<string,SemanticNode>();

			foreach ( SemanticNode comp in pCompareNodes ) {
				map = map.Union(comp.RelationMap);
			}

			return map.ToDictionary(x => x.Key, x => x.Value);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public string GetRelationString() {
			return string.Join("; ", RelationMap.Keys);
		}

		/*--------------------------------------------------------------------------------------------*/
		public override string ToString() {
			return SynSet.WordList.Aggregate("", (x, w) => x+w.Name+", ");
		}

	}

}