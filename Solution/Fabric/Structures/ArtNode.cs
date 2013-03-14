using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data.Domain;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class ArtNode {

		public Artifact Art { get; set; }
		public SemanticNode Node { get; set; }
		public Word Word { get; set; }
		public bool IsWord { get; set; }
		public IDictionary<string, SemanticNode> RelationDiff { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void SetDisambLevel(int pLevel) {
			//string pos = Stats.PartsOfSpeech[Node.SynSet.PartOfSpeechId]+"";

			switch ( pLevel ) {
				case 1:
				case 2:
				case 3:
					Art.Disamb = (IsWord ?
						GetWordHyperDisamb(Node, pLevel, 1) :
						GetSynsetHyperDisamb(Node, pLevel, 1));
					break;

				case 4:
				case 5:
				case 6:
					Art.Disamb = (IsWord ?
						GetWordHyperDisamb(Node, (pLevel-3), 2) :
						GetSynsetHyperDisamb(Node, (pLevel-3), 2));
					break;

				case 7:
				case 8:
					Art.Disamb = (IsWord ?
						GetWordHyperDisamb(Node, 2, (pLevel-6)) :
						GetSynsetHyperDisamb(Node, 2, (pLevel-6)));

					string holo = GetHolonymDisamb(Node, 2);

					if ( holo.Length > 0 ) {
						Art.Disamb += " ["+holo+"]";
					}
					break;

				case 9:
					Art.Disamb = GlossToDisamb(Node.SynSet.Gloss, true);
					//Console.WriteLine("Disamb "+(IsWord ? "w"+Word.Id : "s"+Node.SynSet.Id)+": "+
					//	Art.Name+" // "+Art.Disamb);
					break;

				case 10:
					Art.Disamb = GlossToDisamb(Node.SynSet.Gloss, false);
					//Console.WriteLine("FullDisamb "+(IsWord ? "w"+Word.Id : "s"+Node.SynSet.Id)+": "+
					//	Art.Name+" // "+Art.Disamb);
					break;

				case 11:
					Art.Disamb = Node.SynSet.Gloss;
					//Console.WriteLine("FullDisamb "+(IsWord ? "w"+Word.Id : "s"+Node.SynSet.Id)+": "+
					//	Art.Name+" // "+Art.Disamb);
					break;
			}

			if ( pLevel <= 6 && Node.SynSet.Gloss.IndexOf('(', 0, 1) == 0 ) {
				int endI = Node.SynSet.Gloss.IndexOf(')');
				Art.Disamb += " "+Node.SynSet.Gloss.Substring(0, endI+1);
			}

			/*if ( pLevel == 8 ) {
				Console.WriteLine(" - "+Art.Name+" // "+
					(IsWord ? "w"+Word.Id : "s"+Node.SynSet.Id)+" // "+Art.Disamb);
			}*/
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private string GetSynsetHyperDisamb(SemanticNode pNode, int pWidth, int pDepth) {
			List<SemanticNode> hypers = pNode.Relations[WordNetEngine.SynSetRelation.Hypernym];
			hypers.AddRange(pNode.Relations[WordNetEngine.SynSetRelation.InstanceHypernym]);

			if ( hypers.Count == 0 ) {
				return null;
			}

			string d = "";

			foreach ( SemanticNode hn in hypers ) {
				d += (d == "" ? "" : "; ")+GetWordHyperDisamb(hn, pWidth, pDepth-1);
			}

			if ( pDepth > 1 ) {
				string d2 = "";
				int count = 0;

				foreach ( SemanticNode hn in hypers ) {
					string hd = GetSynsetHyperDisamb(hn, pWidth, pDepth-1);

					if ( hd == null ) {
						continue;
					}

					d2 += (count == 0 ? "" : "; ")+hd;

					if ( ++count >= pWidth ) {
						break;
					}
				}

				d = d2+" > "+d;
			}

			return d;
		}

		/*--------------------------------------------------------------------------------------------*/
		private string GetWordHyperDisamb(SemanticNode pNode, int pWidth, int pDepth) {
			if ( pDepth > 1 ) {
				return GetSynsetHyperDisamb(pNode, pWidth, pDepth-1);
			}

			var dList = new List<string>();
			List<string> words = GetWordList(pNode.SynSet);

			for ( int i = 0 ; i < words.Count && dList.Count < pWidth ; ++i ) {
				if ( words[i] == Art.Name ) {
					continue;
				}

				dList.Add(words[i]);
			}

			return string.Join(", ", dList);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private string GetHolonymDisamb(SemanticNode pNode, int pWidth) {
			List<SemanticNode> holos = pNode.Relations[WordNetEngine.SynSetRelation.MemberHolonym];
			holos.AddRange(pNode.Relations[WordNetEngine.SynSetRelation.PartHolonym]);
			holos.AddRange(pNode.Relations[WordNetEngine.SynSetRelation.SubstanceHolonym]);
			return GetRelationsDisamb(holos, pWidth);
		}

		/*--------------------------------------------------------------------------------------------*/
		private string GetRelationsDisamb(List<SemanticNode> pTargets, int pWidth) {
			if ( pTargets.Count == 0 ) {
				return "";
			}

			var dList = new List<string>();
			var words = new List<string>();

			foreach ( SemanticNode targ in pTargets ) {
				words.AddRange(GetWordList(targ.SynSet));
			}

			for ( int i = 0 ; i < words.Count && dList.Count < pWidth ; ++i ) {
				if ( words[i] == Art.Name ) {
					continue;
				}

				dList.Add(words[i]);
			}

			return string.Join(", ", dList);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static List<String> GetWordList(Synset pSynset) {
			var words = new List<string>();

			foreach ( Word w in pSynset.WordList ) {
				words.Add(FixWordName(w));
			}

			return words;
		}

		/*--------------------------------------------------------------------------------------------*/
		public static string FixWordName(Word pWord) {
			return pWord.Name.Replace('_', ' ');
		}

		/*--------------------------------------------------------------------------------------------*/
		public static string TruncateString(string pString, int pMaxLen) {
			if ( pString == null ) {
				return null;
			}

			if ( pString.Length <= pMaxLen ) {
				return pString;
			}

			return pString.Substring(0, pMaxLen-3)+"...";
		}

		/*--------------------------------------------------------------------------------------------*/
		public static string GlossToDisamb(string pGloss, bool pTruncate) {
			string d = pGloss+"";
			int endI = d.IndexOf(";");

			if ( pTruncate && endI != -1 ) {
				d = d.Substring(0, endI);
			}

			if ( d.IndexOf("the ") == 0 ) {
				d = d.Substring(4);
			}

			if ( d.IndexOf("a ") == 0 ) {
				d = d.Substring(2);
			}

			if ( d.IndexOf("an ") == 0 ) {
				d = d.Substring(3);
			}

			d = Regex.Replace(d, "; \"(.*?)\"", "");
			return d;
		}

	}

}