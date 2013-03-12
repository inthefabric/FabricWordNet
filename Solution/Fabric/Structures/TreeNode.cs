using System.Collections.Generic;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class TreeNode {

		public Synset SynSet { get; private set; }
		public IList<TreeNode> Hypernyms { get; private set; }
		public IList<TreeNode> Hyponyms { get; private set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public TreeNode(Synset pSynSet) {
			SynSet = pSynSet;
			Hypernyms = new List<TreeNode>();
			Hyponyms = new List<TreeNode>();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public override string ToString() {
			return ToString(0, 0);
		}

		/*--------------------------------------------------------------------------------------------*/
		public string ToString(int pDepth, int pMaxDepth) {
			string s = new string(' ', pDepth*4)+(pDepth == 0 ? "" : "- ")+
				SynSet.WordList.Aggregate("", (x, w) => x+w.Name+", ");//+"("+SynSet.SsId+")";

			if ( pDepth == pMaxDepth ) {
				return s;
			}

			foreach ( TreeNode n in Hyponyms ) {
				s += "\n"+n.ToString(pDepth+1, pMaxDepth);
			}

			return s;
		}

	}

}