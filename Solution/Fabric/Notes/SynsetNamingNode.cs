using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Apps.WordNet.Notes {

	/*================================================================================================*/
	public class SynsetNamingNode {

		public SynsetNamingNode ParentNode { get; private set; }
		public string Name { get; private set; }
		public int Depth { get; private set; }
		public List<SynsetRelation> SynRels { get; private set; }
		public List<SynsetNamingNode> ChildNodes { get; private set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SynsetNamingNode(SynsetNamingNode pParentNode, string pName, int pDepth) {
			ParentNode = pParentNode;
			Name = pName;
			Depth = pDepth;
			SynRels = new List<SynsetRelation>();
			ChildNodes = new List<SynsetNamingNode>();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void ConvertSynNamesIntoChildNodes() {
			if ( SynRels.Count == 0 ) {
				return;
			}

			var childMap = new Dictionary<string, SynsetNamingNode>();

			for ( int i = (SynRels.Count > 1 ? SynRels.Count-1 : -1) ; i >= 0 ; i-- ) {
				SynsetRelation synRel = SynRels[i];

				if ( Depth >= synRel.AllNames.Count ) {
					continue;
				}

				string childName = synRel.AllNames[Depth].ToLower();

				if ( !childMap.ContainsKey(childName) ) {
					var childNode = new SynsetNamingNode(this, childName, Depth+1);
					childMap.Add(childName, childNode);
					ChildNodes.Add(childNode);
				}

				childMap[childName].SynRels.Add(synRel);
				SynRels.RemoveAt(i);
			}

			if ( SynRels.Count == 1 ) {
				SynRels[0].Synset.UniqueParts = ToUniqueParts();
			}

			foreach ( SynsetNamingNode childNode in ChildNodes ) {
				childNode.ConvertSynNamesIntoChildNodes();
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private bool IsSilent() {
			if ( ParentNode?.ChildNodes.Count != 1 ) {
				return false;
			}

			if ( ChildNodes.Count == 1 ) {
				return true;
			}

			foreach ( SynsetNamingNode childNode in ChildNodes ) {
				if ( childNode.ChildNodes.Count > 0 ) {
					return false; //at least one child has its own children
				}
			}

			return true;
		}

		/*--------------------------------------------------------------------------------------------*/
		private List<string> ToUniqueParts() {
			var parts = new List<string>();
			parts.Add(Name);

			SynsetNamingNode node = ParentNode;

			while ( node != null ) {
				if ( !node.IsSilent() ) {
					parts.Insert(0, node.Name);
				}

				node = node.ParentNode;
			}

			return parts;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public override string ToString() {
			return $"Node[{Depth}:{Name}:{SynRels.Count}:{ChildNodes.Count}]";
		}

		/*--------------------------------------------------------------------------------------------*/
		public string ToTreeString() {
			var sb = new StringBuilder();

			for ( int i = 0 ; i < Depth ; i++ ) {
				sb.Append("|   ");
			}

			if ( IsSilent() ) {
				sb.Append('(');
				sb.Append(Name);
				sb.Append(')');
			}
			else {
				sb.Append(Name);
			}

			if ( SynRels.Count > 1 ) {
				sb.Append(" <DUPLICATE>");
			}
			else if ( SynRels.Count == 1 ) {
				List<string> parts = ToUniqueParts();

				sb.Append("   @");
				sb.Append(string.Join(".", parts));
				sb.Append(' ');
				sb.Append('[');
				sb.Append('#', Math.Max(0, parts.Count-2));
				sb.Append(']');
			}

			sb.Append('\n');

			foreach ( SynsetNamingNode childNode in ChildNodes ) {
				sb.Append(childNode.ToTreeString());
			}

			return sb.ToString();
		}

	}

}
