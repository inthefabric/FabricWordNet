using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data.Domain;
using static Fabric.Apps.WordNet.WordNetEngine;

namespace Fabric.Apps.WordNet.Notes {

	/*================================================================================================*/
	public class SynsetRelation {

		private static readonly string[] SynsetPosNames = { "", "noun", "verb", "adj", "adv" };

		public Synset Synset { get; private set; }
		public string PartOfSpeechText { get; private set; }

		public List<Word> Members { get; private set; }
		public List<Word> Hypernyms { get; private set; }
		public List<Word> Holonnyms { get; private set; }
		public List<Word> Entailments { get; private set; }
		public List<Word> Domains { get; private set; }
		public List<Word> Causes { get; private set; }
		public List<Word> VerbGroups { get; private set; }
		public List<Word> Attributes { get; private set; }
		public List<Word> Similars { get; private set; }
		//public List<Word> AlsoSees { get; private set; }
		public List<Word> LexPertains { get; private set; }
		public List<Word> LexRelates { get; private set; }
		public List<Word> LexDerives { get; private set; }
		public List<string> GlossMajors { get; private set; }
		public List<string> GlossMinors { get; private set; }

		public List<string> AllNames { get; private set; }

		public string MembersText { get; private set; }
		public string HypernymsText { get; private set; }
		public string HolonnymsText { get; private set; }
		public string EntailmentsText { get; private set; }
		public string DomainsText { get; private set; }
		public string CausesText { get; private set; }
		public string VerbGroupsText { get; private set; }
		public string AttributesText { get; private set; }
		public string SimilarText { get; private set; }
		//public string AlsoSeeText { get; private set; }
		public string LexPertainsText { get; private set; }
		public string LexRelatesText { get; private set; }
		public string LexDerivesText { get; private set; }
		public string GlossMajorsText { get; private set; }
		public string GlossMinorsText { get; private set; }
		public string GlossNoExamplesText { get; private set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SynsetRelation(Synset pSynset) {
			Synset = pSynset;
			PartOfSpeechText = SynsetPosNames[Synset.PartOfSpeechId];

			Members = Synset.WordList.ToList(); //copy
			Hypernyms = GetSemanticWords(pSynset, IsHypernym);
			Holonnyms = GetSemanticWords(pSynset, IsHolonym);
			Entailments = GetSemanticWords(pSynset, IsEntailment);
			Domains = GetSemanticWords(pSynset, IsDomain);
			Causes = GetSemanticWords(pSynset, IsCause);
			VerbGroups = GetSemanticWords(pSynset, IsVerbGroup);
			Attributes = GetSemanticWords(pSynset, IsAttribute);
			Similars = GetSemanticWords(pSynset, IsSimilar);
			//AlsoSees = GetSemanticWords(pSynset, IsAlsoSee);
			LexPertains = GetLexicalWords(pSynset, IsLexPertains);
			LexRelates = GetLexicalWords(pSynset, IsLexRelates);
			LexDerives = GetLexicalWords(pSynset, IsLexDerived);
			GlossMajors = GetGlossWords(Synset.Gloss, out List<string> minors, out string noExamples);
			GlossMinors = minors;

			AllNames = BuildAllNames();

			MembersText = BuildText(Members);
			HypernymsText = BuildText(Hypernyms);
			HolonnymsText = BuildText(Holonnyms);
			EntailmentsText = BuildText(Entailments);
			DomainsText = BuildText(Domains);
			CausesText = BuildText(Causes);
			VerbGroupsText = BuildText(VerbGroups);
			AttributesText = BuildText(Attributes);
			SimilarText = BuildText(Similars);
			//AlsoSeeText = BuildText(AlsoSees);
			LexPertainsText = BuildText(LexPertains);
			LexRelatesText = BuildText(LexRelates);
			LexDerivesText = BuildText(LexDerives);
			GlossMajorsText = string.Join("|", GlossMajors.Distinct());
			GlossMinorsText = string.Join("|", GlossMinors.Distinct());
			GlossNoExamplesText = noExamples;

			//TODO: be able to compare multiple gloss and pick out the first significant difference
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static List<Synset> GetSemanticSynsets(Synset pSynset, 
												Func<Semantic, bool> pWhere, bool pTowardTarget=true) {
			return (pTowardTarget ? 
				pSynset.SemanticList.Where(pWhere).Select(s => s.TargetSynset).ToList() :
				pSynset.SemanticTargetList.Where(pWhere).Select(s => s.Synset).ToList());
		}

		/*--------------------------------------------------------------------------------------------*/
		private static List<Synset> GetLexicalSynsets(Synset pSynset, 
												Func<Lexical, bool> pWhere, bool pTowardTarget=true) {
			return (pTowardTarget ? 
				pSynset.LexicalList.Where(pWhere).Select(s => s.TargetSynset).ToList() :
				pSynset.LexicalTargetList.Where(pWhere).Select(s => s.Synset).ToList());
		}

		/*--------------------------------------------------------------------------------------------*/
		private static List<Word> GetSemanticWords(Synset pSynset, Func<Semantic, bool> pWhere) {
			List<Word> words = GetSemanticSynsets(pSynset, pWhere).SelectMany(s => s.WordList).ToList();
			words.Sort(NotePrep.SortWordsByWordnameCount);
			return words;
		}

		/*--------------------------------------------------------------------------------------------*/
		private static List<Word> GetLexicalWords(Synset pSynset, Func<Lexical, bool> pWhere) {
			List<Word> words = GetLexicalSynsets(pSynset, pWhere).SelectMany(s => s.WordList).ToList();
			words.Sort(NotePrep.SortWordsByWordnameCount);
			return words;
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsHypernym(Semantic pSemantic) {
			SynSetRelation rel = (SynSetRelation)pSemantic.RelationId;
			return (
				rel == SynSetRelation.Hypernym ||  
				rel == SynSetRelation.InstanceHypernym
			);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsHolonym(Semantic pSemantic) {
			SynSetRelation rel = (SynSetRelation)pSemantic.RelationId;
			return (
				rel == SynSetRelation.MemberHolonym ||  
				rel == SynSetRelation.PartHolonym ||  
				rel == SynSetRelation.SubstanceHolonym
			);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsEntailment(Semantic pSemantic) {
			return ((SynSetRelation)pSemantic.RelationId == SynSetRelation.Entailment);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsDomain(Semantic pSemantic) {
			SynSetRelation rel = (SynSetRelation)pSemantic.RelationId;
			return (
				rel == SynSetRelation.TopicDomain ||
				rel == SynSetRelation.RegionDomain ||
				rel == SynSetRelation.UsageDomain
			);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsCause(Semantic pSemantic) {
			return ((SynSetRelation)pSemantic.RelationId == SynSetRelation.Cause);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsVerbGroup(Semantic pSemantic) {
			return ((SynSetRelation)pSemantic.RelationId == SynSetRelation.VerbGroup);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsAttribute(Semantic pSemantic) {
			return ((SynSetRelation)pSemantic.RelationId == SynSetRelation.Attribute);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsSimilar(Semantic pSemantic) {
			return ((SynSetRelation)pSemantic.RelationId == SynSetRelation.SimilarTo);
		}

		/*--------------------------------------------------------------------------------------------* /
		private static bool IsAlsoSee(Semantic pSemantic) {
			return ((SynSetRelation)pSemantic.RelationId == SynSetRelation.AlsoSee);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsLexPertains(Lexical pLexical) {
			return ((SynSetRelation)pLexical.RelationId == SynSetRelation.Pertainym);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsLexRelates(Lexical pLexical) {
			return ((SynSetRelation)pLexical.RelationId == SynSetRelation.DerivationallyRelated);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static bool IsLexDerived(Lexical pLexical) {
			return ((SynSetRelation)pLexical.RelationId == SynSetRelation.DerivedFromAdjective);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static List<string> GetGlossWords(string pGloss,
											out List<string> pMinorWords, out string pNoExamples) {
			string g = pGloss;

			int ei = g.IndexOf("; \"");
			pNoExamples = (ei == -1 ? g : g.Substring(0, ei));

			List<string> words = Regex.Split(pNoExamples, @"[^\w]").ToList();
			pMinorWords = new List<string>();

			for ( int i = words.Count-1 ; i >= 0 ; i-- ) {
				string word = words[i];

				if ( word.Length == 0 ) {
					words.RemoveAt(i);
					continue;
				}

				if ( char.IsUpper(word[0]) ) {
					continue;
				}

				bool isMajor = true;

				if ( word.Length < 3 ) {
					isMajor = false;
				}
				else {
					switch ( word ) {
						case "the":
						case "and":
						case "was":
						case "are":
						case "for":
						case "use":
						case "may":
						case "that":
						case "this":
						case "with":
						case "kind":
						case "into":
						case "from":
						case "some":
						case "used":
						case "what":
						case "having":
						case "relating":
						case "denoting":
						case "pertaining":
						case "indicating":
							isMajor = false;
							break;
					}
				}

				if ( isMajor ) {
					continue;
				}

				words.RemoveAt(i);
				
				if ( word.Length > 1 ) {
					pMinorWords.Add(word);
				}
			}

			return words;
		}

		/*--------------------------------------------------------------------------------------------*/
		private List<string> BuildAllNames() {
			var all = new List<string>();
			all.Add(PartOfSpeechText);
			all.AddRange(Members.Select(w => w.Name));
			all.AddRange(Hypernyms.Select(w => w.Name));
			all.AddRange(Holonnyms.Select(w => w.Name));
			all.AddRange(Entailments.Select(w => w.Name));
			all.AddRange(Domains.Select(w => w.Name));
			all.AddRange(Causes.Select(w => w.Name));
			all.AddRange(VerbGroups.Select(w => w.Name));
			all.AddRange(Attributes.Select(w => w.Name));
			all.AddRange(Similars.Select(w => w.Name));
			//AllNames.AddRange(AlsoSees.Select(w => w.Name));
			all.AddRange(LexPertains.Select(w => w.Name));
			all.AddRange(LexRelates.Select(w => w.Name));
			all.AddRange(LexDerives.Select(w => w.Name));
			all.AddRange(GlossMajors);
			all.AddRange(GlossMinors);
			all = all.Distinct().ToList();

			//remove british variations (like "organized" vs. "organised")

			for ( int a = all.Count-1 ; a >= 0 ; a-- ) {
				string allName = all[a];

				for ( int c = 2 ; c < allName.Length ; c++ ) {
					if ( allName[c] != 's' ) {
						continue;
					}

					string fixName = allName.Remove(c, 1).Insert(c, "z");

					if ( all.Contains(fixName) ) {
						all.RemoveAt(a);
					}
				}
			}

			return all;
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string BuildText(List<Word> pWords) {
			return (pWords.Count == 0 ? null :
				string.Join("|", pWords.Select(w => w.Name).Distinct()));
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public override string ToString() {
			const char sep = ',';

			return MembersText+
				sep+(HypernymsText 		!= null ? " [H] "+HypernymsText : "")+
				sep+(HolonnymsText		!= null ? " [O] "+HolonnymsText : "")+
				sep+(EntailmentsText 	!= null ? " [E] "+EntailmentsText : "")+
				sep+(DomainsText		!= null ? " [D] "+DomainsText : "")+
				sep+(CausesText			!= null ? " [C] "+CausesText : "")+
				sep+(VerbGroupsText		!= null ? " [V] "+VerbGroupsText : "")+
				sep+(AttributesText		!= null ? " [A] "+AttributesText : "")+
				sep+(SimilarText		!= null ? " [S] "+SimilarText : "")+
				//sep+(AlsoSeeText		!= null ? " [L] "+AlsoSeeText : "")+
				sep+(LexPertainsText	!= null ? " [LP] "+LexPertainsText : "")+
				sep+(LexRelatesText		!= null ? " [LR] "+LexRelatesText : "")+
				sep+(LexDerivesText		!= null ? " [LD] "+LexDerivesText : "")+
				sep+" [GM] "+GlossMajorsText+" / "+GlossMinorsText+" { "+GlossNoExamplesText+" }";
		}

	}

}
