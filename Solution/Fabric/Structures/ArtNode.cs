using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data.Domain;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class ArtNode {

		public Artifact Art { get; set; }
		public SemanticNode Node { get; set; }
		public Word Word { get; set; }
		public bool IsWord { get; set; }

		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public HashSet<string> GetWordMap() {
			var wordMap = new HashSet<string>();

			if ( IsWord ) {
				foreach ( Word w in Node.SynSet.WordList ) {
					wordMap.Add(FixWordName(w));
				}
			}

			List<SemanticNode> rels = Node.Relations[WordNetEngine.SynSetRelation.Hypernym];
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.InstanceHypernym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.SubstanceHolonym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.PartHolonym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.MemberHolonym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.UsageDomain]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.TopicDomain]);

			foreach ( SemanticNode sn in rels ) {
				foreach ( Word w in sn.SynSet.WordList ) {
					wordMap.Add(FixWordName(w));
				}
			}

			wordMap.Add("["+Stats.PartsOfSpeech[Node.SynSet.PartOfSpeechId]+"]");
			return wordMap;
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public string GetUniqueWordString(List<ArtNode> pDupSet) {
			IEnumerable<string> dupSetMap = new HashSet<string>();

			foreach ( ArtNode dup in pDupSet ) {
				if ( dup != this ) {
					dupSetMap = dupSetMap.Union(dup.GetWordMap());
				}
			}

			HashSet<string> nodeWords = GetWordMap();
			
			if ( nodeWords.Count == 0 ) {
				return null;
			}

			List<string> words = nodeWords.Except(dupSetMap).ToList();

			if ( words.Count == 0 ) {
				return null;
			}

			string pos = words.Last();
			pos = (pos.IndexOf('[') == 0 ? pos : null);

			int max = Math.Min(words.Count, (pos == null ? 2 : 3));
			words = words.GetRange(0, max);
			return string.Join(", ", words);
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