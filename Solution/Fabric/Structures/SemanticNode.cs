using System.Collections.Generic;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class SemanticNode {

		public Synset SynSet { get; private set; }
		public IDictionary<WordNetEngine.SynSetRelation, List<SemanticNode>>
																		Relations { get; private set; }
		

		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SemanticNode(Synset pSynSet) {
			SynSet = pSynSet;
			Relations = new Dictionary<WordNetEngine.SynSetRelation, List<SemanticNode>>();

			foreach ( WordNetEngine.SynSetRelation ssr in Stats.Relations ) {
				Relations.Add(ssr, new List<SemanticNode>());
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public void AddRelation(WordNetEngine.SynSetRelation pRel, SemanticNode pTargNode) {
			Relations[pRel].Add(pTargNode);
		}

		/*--------------------------------------------------------------------------------------------*/
		public override string ToString() {
			return SynSet.WordList.Aggregate("", (x, w) => x+w.Name+", ");
		}

	}

}