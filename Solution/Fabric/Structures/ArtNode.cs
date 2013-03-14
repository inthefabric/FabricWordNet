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
		public HashSet<string> WordMap { get; set; }

		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public HashSet<string> GetDisambWordMap(bool pIncludeHypers) {
			if ( WordMap != null ) {
				return WordMap;
			}

			WordMap = new HashSet<string>();
			const int max = 3;

			if ( IsWord ) {
				foreach ( Word w in Node.SynSet.WordList ) {
					if ( w.Name != Art.Name ) {
						WordMap.Add(FixWordName(w));
					}
				}
			}

			//use ToList() to make a copy -- don't modify the Node.Relations list!
			List<SemanticNode> rels = Node.Relations[WordNetEngine.SynSetRelation.Hypernym].ToList();
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.InstanceHypernym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.Pertainym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.VerbGroup]);

			if ( pIncludeHypers ) {
				List<SemanticNode> hyperRels = rels.ToList();

				foreach ( SemanticNode hyper in hyperRels ) {
					rels.AddRange(hyper.Relations[WordNetEngine.SynSetRelation.Hypernym]);
					rels.AddRange(hyper.Relations[WordNetEngine.SynSetRelation.InstanceHypernym]);
				}
			}

			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.SubstanceHolonym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.PartHolonym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.MemberHolonym]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.Entailment]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.UsageDomain]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.UsageDomainMember]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.TopicDomain]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.TopicDomainMember]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.RegionDomain]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.RegionDomainMember]);
			rels.AddRange(Node.Relations[WordNetEngine.SynSetRelation.SimilarTo]);

			rels.AddRange(Node.Lexicals[WordNetEngine.SynSetRelation.DerivationallyRelated]);
			rels.AddRange(Node.Lexicals[WordNetEngine.SynSetRelation.DerivedFromAdjective]);
			rels.AddRange(Node.Lexicals[WordNetEngine.SynSetRelation.Pertainym]);
			rels.AddRange(Node.Lexicals[WordNetEngine.SynSetRelation.SimilarTo]);

			foreach ( SemanticNode sn in rels ) {
				foreach ( Word w in sn.SynSet.WordList ) {
					if ( w.Name != Art.Name ) {
						WordMap.Add(FixWordName(w));
					}
				}
			}

			List<SemanticNode> ants = Node.Lexicals[WordNetEngine.SynSetRelation.Antonym];

			foreach ( SemanticNode sn in ants ) {
				foreach ( Word w in sn.SynSet.WordList ) {
					WordMap.Add("not "+FixWordName(w));
				}
			}

			return WordMap;
		}

		/*--------------------------------------------------------------------------------------------*/
		public string GetSimpleDisambString(bool pIncludeHypers) {
			List<string> words = GetDisambWordMap(pIncludeHypers).ToList();

			if ( words.Count == 0 ) {
				string test = "FIX: ";

				foreach ( KeyValuePair<WordNetEngine.SynSetRelation, List<SemanticNode>> pair 
						in Node.Relations ) {
					foreach ( SemanticNode sn in pair.Value ) {
						test += pair.Key+"|"+sn.SynSet.Id+"; ";
					}
				}

				return test;
			}

			words = words.GetRange(0, Math.Min(words.Count, 3));
			return string.Join(", ", words);
		}

		/*--------------------------------------------------------------------------------------------*/
		public string GetUniqueDisambString(List<ArtNode> pDupSet, bool pIncludeHypers) {
			//Console.WriteLine(" - guds() A: "+Art.Name);
			IEnumerable<string> dupSetMap = new HashSet<string>();

			foreach ( ArtNode dup in pDupSet ) {
				if ( dup != this ) {
					dupSetMap = dupSetMap.Union(dup.GetDisambWordMap(pIncludeHypers));
				}
			}
			//Console.WriteLine(" - guds() B");
			HashSet<string> nodeWordMap = GetDisambWordMap(pIncludeHypers);
			List<string> words = (nodeWordMap.Count == 0 ? 
				new List<string>() : nodeWordMap.Except(dupSetMap).ToList());

			//Console.WriteLine(" - guds() C");
			if ( words.Count >= 3 ) {
				words = words.GetRange(0, 3);
			}
			else {
				List<string> nodeWords = nodeWordMap.ToList();

				while ( words.Count < 3 && nodeWords.Count > 0 ) {
					words.Add(nodeWords[0]);
					nodeWords.RemoveAt(0);
				}
			}

			//Console.WriteLine(" - guds() D");
			return string.Join(", ", words);
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public string GetGlossString(int pSize, bool pShowPos) {
			string str = "";

			switch ( pSize ) {
				case 1:
					str += GlossToDisamb(Node.SynSet.Gloss, true);
					break;

				case 2:
					str += GlossToDisamb(Node.SynSet.Gloss, false);
					break;

				case 3:
					str += Node.SynSet.Gloss;
					break;
			}

			if ( pShowPos ) {
				str += " ["+Stats.PartsOfSpeech[Node.SynSet.PartOfSpeechId]+"]";
			}

			return str;
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