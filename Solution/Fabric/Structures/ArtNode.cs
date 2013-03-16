using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data.Domain;

namespace Fabric.Apps.WordNet.Structures {

	/*================================================================================================*/
	public class ArtNode {

		public enum DisambType {
			Unique = 1,
			Simple,
			Gloss
		}

		public Artifact Art { get; set; }
		public SemanticNode Node { get; set; }
		public Word Word { get; set; }
		public bool IsWord { get; set; }

		public HashSet<string> WordMap { get; set; }
		public DisambType DisType { get; set; }
		public int DisVal { get; set; }
		public int DisCount { get; set; }
		public int FillCount { get; set; }
		public bool IsFinal { get; set; }

		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public HashSet<string> GetDisambWordMap(bool pIncludeHypers, ArtNode pRequester=null) {
			if ( WordMap != null ) {
				return WordMap;
			}

			WordMap = new HashSet<string>();

			if ( IsWord ) {
				foreach ( Word w in Node.SynSet.WordList ) {
					string fix = FixWordName(w);

					if ( fix != Art.Name ) {
						WordMap.Add(fix);
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
					if ( pRequester != null && hyper == pRequester.Node ) {
						continue;
					}

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
				if ( pRequester != null && sn == pRequester.Node ) {
					continue;
				}

				foreach ( Word w in sn.SynSet.WordList ) {
					string fix = FixWordName(w);

					if ( fix != Art.Name ) {
						WordMap.Add(fix);
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
		private void SetDisamb(string pDisamb) {
			string pos = " ["+Stats.PartsOfSpeech[Node.SynSet.PartOfSpeechId]+"]";
			Art.Disamb = TruncateString(pDisamb, 128-pos.Length)+pos;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void SetSimpleDisambString() {
			List<string> words = GetDisambWordMap(false).ToList();

			if ( words.Count == 0 ) {
				SetGlossString(1);
				DisType = DisambType.Simple;
				DisVal = 0;
				return;
			}

			DisType = DisambType.Simple;
			DisVal = 1;

			if ( words.Count < 3 ) {
				words = GetDisambWordMap(true).ToList();
				DisVal = 2;
			}

			words = words.GetRange(0, Math.Min(words.Count, 3));
			SetDisamb(string.Join(" / ", words));
			DisCount = words.Count;
		}

		/*--------------------------------------------------------------------------------------------*/
		public void SetUniqueDisambString(List<ArtNode> pDupSet) {
			IEnumerable<string> dupSetMap = new HashSet<string>();
			DisType = DisambType.Unique;
			DisVal = 1;

			foreach ( ArtNode dup in pDupSet ) {
				if ( dup != this ) {
					dupSetMap = dupSetMap.Union(dup.GetDisambWordMap(true, this));
				}
			}

			HashSet<string> nodeWordMap = GetDisambWordMap(true);
			List<string> words = (nodeWordMap.Count == 0 ? 
				new List<string>() : nodeWordMap.Except(dupSetMap).ToList());
			
			if ( words.Count == 0 ) {
				SetGlossString(1);
				DisType = DisambType.Unique;
				DisVal = 0;
				return;
			}

			if ( words.Count >= 3 ) {
				words = words.GetRange(0, 3);
				DisVal = 2;
			}

			SetDisamb(string.Join(" / ", words));
			DisCount = words.Count;
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public void SetGlossString(int pSize) {
			DisType = DisambType.Gloss;
			DisVal = pSize;
			DisCount = -1;

			switch ( pSize ) {
				case 1:
					SetDisamb(GlossToDisamb(Node.SynSet.Gloss, true));
					return;

				case 2:
					SetDisamb(GlossToDisamb(Node.SynSet.Gloss, false));
					return;

				case 3:
					SetDisamb(Node.SynSet.Gloss);
					return;
			}

			throw new Exception("Invalid GlossString size: "+pSize);
		}

		/*--------------------------------------------------------------------------------------------*/
		public void EnsureFilledDisamb() {
			if ( DisCount == -1 || DisCount >= 3 ) {
				return;
			}
			
			List<string> nodeWords = GetDisambWordMap(false).ToList();
			FillCount = Math.Min(3-DisCount, nodeWords.Count);

			for ( int i = 0 ; i < FillCount ; ++i ) {
				Art.Disamb += (i == 0 && DisCount == 0 ? "" : " / ")+nodeWords[i];
			}
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
			return pWord.Name
				.Replace('_', ' ')
				.Replace("(a)", "")
				.Replace("(p)", "");
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