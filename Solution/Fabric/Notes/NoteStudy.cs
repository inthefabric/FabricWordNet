using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using NHibernate;
using static Fabric.Apps.WordNet.WordNetEngine;

namespace Fabric.Apps.WordNet.Notes {

	/*================================================================================================*/
	public static class NoteStudy {

		private struct WordnameSynsets {

			public string Name;
			public List<Synset> Synsets;

		}

		private struct SynsetName {

			public Synset Synset;

			public List<Word> Members;
			public List<Word> Hypernyms;
			public List<Word> Holonnyms;
			public List<Word> Entailments;
			public List<Word> Domains;
			public List<Word> Causes;
			public List<Word> VerbGroups;
			public List<Word> Attributes;
			public List<Word> Similars;
			//public List<Word> AlsoSees;
			public List<Word> LexPertains;
			public List<Word> LexRelates;
			public List<Word> LexDerives;
			public List<string> GlossMajors;
			public List<string> GlossMinors;
			public List<string> AllNames;

			public string PartOfSpeechText;
			public string GlossNoExamplesText;

			public string MembersText;
			public string HypernymsText;
			public string HolonnymsText;
			public string EntailmentsText;
			public string DomainsText;
			public string CausesText;
			public string VerbGroupsText;
			public string AttributesText;
			public string SimilarText;
			//public string AlsoSeeText;
			public string LexPertainsText;
			public string LexRelatesText;
			public string LexDerivesText;
			public string GlossMajorsText;
			public string GlossMinorsText;

			public SynsetName(Synset pSynset) {
				Synset = pSynset;

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

				PartOfSpeechText = SynsetPosNames[Synset.PartOfSpeechId];
				GlossMajors = GetGlossWords(Synset.Gloss,
					out GlossMinors, out GlossNoExamplesText);

				AllNames = new List<string>();
				AllNames.Add(PartOfSpeechText);
				AllNames.AddRange(Members.Select(w => w.Name));
				AllNames.AddRange(Hypernyms.Select(w => w.Name));
				AllNames.AddRange(Holonnyms.Select(w => w.Name));
				AllNames.AddRange(Entailments.Select(w => w.Name));
				AllNames.AddRange(Domains.Select(w => w.Name));
				AllNames.AddRange(Causes.Select(w => w.Name));
				AllNames.AddRange(VerbGroups.Select(w => w.Name));
				AllNames.AddRange(Attributes.Select(w => w.Name));
				AllNames.AddRange(Similars.Select(w => w.Name));
				//AllNames.AddRange(AlsoSees.Select(w => w.Name));
				AllNames.AddRange(LexPertains.Select(w => w.Name));
				AllNames.AddRange(LexRelates.Select(w => w.Name));
				AllNames.AddRange(LexDerives.Select(w => w.Name));
				AllNames.AddRange(GlossMajors);
				AllNames.AddRange(GlossMinors);
				AllNames = AllNames.Distinct().ToList();

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

				//TODO: be able to compare multiple gloss and pick out the first significant difference
			}

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
							case "that":
							case "this":
							case "with":
							case "kind":
							case "into":
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

			private static string BuildText(List<Word> pWords) {
				return (pWords.Count == 0 ? null :
					string.Join("|", pWords.Select(w => w.Name).Distinct()));
			}

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

		private static readonly string[] SynsetPosAbbrevs = { "", "n", "v", "a", "b" };
		private static readonly string[] SynsetPosNames = { "", "noun", "verb", "adj", "adv" };

		private static List<Synset> SynsetList;
		private static List<Word> WordList;
		private static IList<Semantic> SemanticList;
		private static IList<Lexical> LexicalList;

		private static Dictionary<string, List<Synset>> WordnameSynsetsMap;
		private static List<WordnameSynsets> WordameSynsetList;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void Test() {
			Console.WriteLine("Study...\n");
			Stopwatch timer = Stopwatch.StartNew();
			DbBuilder.UpdateSchema();

			using ( ISession sess = new SessionProvider().OpenSession() ) {
				sess.CacheMode = CacheMode.Get;
				sess.FlushMode = FlushMode.Manual;

				Console.WriteLine("GetSynsets...");
				SynsetList = sess.QueryOver<Synset>().List().ToList();
				Console.WriteLine("GetWords...");
				WordList = sess.QueryOver<Word>().List().ToList();
				Console.WriteLine("GetSemantics...");
				SemanticList = sess.QueryOver<Semantic>().List();
				Console.WriteLine("GetLexicals...");
				LexicalList = sess.QueryOver<Lexical>().List();
			}

			FillReferences();
			SortWords();

			//CalcUniqueSynsetNames();
			PerformSynsetUniqueTree();

			Console.WriteLine($"\nStudy complete: {timer.Elapsed.TotalSeconds} sec");
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void FillReferences() {
			Console.WriteLine("FillReferences...");

			foreach ( Synset synset in SynsetList ) {
				synset.WordList = new List<Word>();
				synset.SemanticList = new List<Semantic>();
				synset.SemanticTargetList = new List<Semantic>();
				synset.LexicalList = new List<Lexical>();
				synset.LexicalTargetList = new List<Lexical>();
			}

			foreach ( Word word in WordList ) {
				string name = word.Name.ToLower();
				name = Regex.Replace(name, @"\(.+\)", ""); //fix "galore(ip)", "afloat(p)", etc.
				name = Regex.Replace(name, @"[\s_]+", "-");
				name = Regex.Replace(name, @"[^\w\-]+", "");

				word.Name = name;
				word.LexicalList = new List<Lexical>();
				word.TargetLexicalList = new List<Lexical>();

				word.Synset.WordList.Add(word);
			}

			foreach ( Semantic semantic in SemanticList ) {
				semantic.Synset.SemanticList.Add(semantic);
				semantic.TargetSynset.SemanticTargetList.Add(semantic);
			}

			foreach ( Lexical lexical in LexicalList ) {
				lexical.Synset.LexicalList.Add(lexical);
				lexical.Word.LexicalList.Add(lexical);

				lexical.TargetSynset.LexicalTargetList.Add(lexical);
				lexical.TargetWord.TargetLexicalList.Add(lexical);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void SortWords() {
			Console.WriteLine("SortWords...");

			WordnameSynsetsMap = new Dictionary<string, List<Synset>>();

			foreach ( Word word in WordList ) {
				string wordname = word.Name;

				if ( !WordnameSynsetsMap.ContainsKey(wordname) ) {
					WordnameSynsetsMap.Add(wordname, new List<Synset>());
				}

				WordnameSynsetsMap[wordname].Add(word.Synset);
			}

			WordameSynsetList = WordnameSynsetsMap
				.Select(p => new WordnameSynsets { Name = p.Key, Synsets = p.Value })
				.ToList();

			WordameSynsetList.Sort((a,b) => b.Synsets.Count-a.Synsets.Count);

			foreach ( Synset synset in SynsetList ) {
				List<Word> wordList = (List<Word>)synset.WordList;

				wordList.Sort((a,b) => 
					WordnameSynsetsMap[a.Name].Count-WordnameSynsetsMap[b.Name].Count);

				/*POS pos = (POS)synset.PartOfSpeechId;

				if ( pos != POS.Noun ) {
					continue;
				}

				int semHypernymCount = synset.SemanticList
					.Count(s => (
						s.RelationId == (byte)SynSetRelation.Hypernym ||
						s.RelationId == (byte)SynSetRelation.InstanceHypernym /*||
						s.RelationId == (byte)SynSetRelation.Entailment ||
						s.RelationId == (byte)SynSetRelation.TopicDomain* /
					));

				if ( semHypernymCount == 0 ) {
					Console.WriteLine("* "+synset.ToDebugString());

					foreach ( Semantic semantic in synset.SemanticList ) {
						SynSetRelation rel = (SynSetRelation)semantic.RelationId;

						switch ( rel ) {
							case SynSetRelation.Hyponym:
							case SynSetRelation.InstanceHyponym:
							case SynSetRelation.SimilarTo:
							case SynSetRelation.AlsoSee:
								continue;

							default:
								Console.WriteLine("    "+rel+": "+
									semantic.TargetSynset.ToDebugString());
								break;
						}
					}
				}*/
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static string ToDebugString(this Synset pSynset) {
			string gloss = pSynset.Gloss;
			int glossEndI = gloss.IndexOf("; \"");

			if ( glossEndI != -1 ) {
				gloss = gloss.Substring(0, glossEndI);
			}

			return SynsetPosAbbrevs[pSynset.PartOfSpeechId]+"|"+
				string.Join("/", pSynset.WordList.Select(w => w.Name))+" ["+gloss+"]";
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
			words.Sort((a,b) => 
				WordnameSynsetsMap[a.Name].Count-WordnameSynsetsMap[b.Name].Count);
			return words;
		}

		/*--------------------------------------------------------------------------------------------*/
		private static List<Word> GetLexicalWords(Synset pSynset, Func<Lexical, bool> pWhere) {
			List<Word> words = GetLexicalSynsets(pSynset, pWhere).SelectMany(s => s.WordList).ToList();
			words.Sort((a,b) => 
				WordnameSynsetsMap[a.Name].Count-WordnameSynsetsMap[b.Name].Count);
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
		/*--------------------------------------------------------------------------------------------* /
		private static void FindSynsetsBasedOnRelations() {
			Console.WriteLine("FindSynsetsBasedOnRelations...");

			foreach ( Synset synset in SynsetList ) {
				POS pos = (POS)synset.PartOfSpeechId;

				if ( pos != POS.Noun ) {
					continue;
				}

				int semHypernymCount = synset.SemanticList
					.Count(s => (
						s.RelationId == (byte)SynSetRelation.Hypernym ||
						s.RelationId == (byte)SynSetRelation.InstanceHypernym /*||
						s.RelationId == (byte)SynSetRelation.Entailment ||
						s.RelationId == (byte)SynSetRelation.TopicDomain* /
					));

				if ( semHypernymCount > 0 ) {
					continue;
				}

				Console.WriteLine("* "+synset.ToDebugString());

				foreach ( Semantic semantic in synset.SemanticList ) {
					SynSetRelation rel = (SynSetRelation)semantic.RelationId;

					switch ( rel ) {
						case SynSetRelation.Hyponym:
						case SynSetRelation.InstanceHyponym:
						case SynSetRelation.SimilarTo:
						case SynSetRelation.AlsoSee:
							continue;
					}

					Console.WriteLine("    "+rel+": "+semantic.TargetSynset.ToDebugString());
				}
			}
		}
		
		/*--------------------------------------------------------------------------------------------* /
		private static void PrintMostFrequentWords() {
			Console.WriteLine("PrintMostFrequentWords...");

			for ( int i = 0 ; i < 10 ; i++ ) {
				WordnameSynsets ws = WordameSynsetList[i];
				Console.WriteLine("\n"+ws.Name+" => "+ws.Synsets.Count);

				foreach ( Synset synset in ws.Synsets ) {
					Console.WriteLine("    "+synset.ToDebugString());

					foreach ( Semantic semantic in synset.SemanticList ) {
						SynSetRelation rel = (SynSetRelation)semantic.RelationId;

						if ( rel != SynSetRelation.Hypernym ) {
							continue;
						}

						Console.WriteLine("        "+semantic.TargetSynset.ToDebugString());
					}
				}
			}
		}*/

		/*--------------------------------------------------------------------------------------------* /
		private static void CalcUniqueSynsetNames() {
			Console.WriteLine("CalcUniqueSynsetNames...");

			var synsetHitMap = new HashSet<int>();
			var synsetNameMap = new Dictionary<string, Synset>();
			var synsetQueue = new Queue<Synset>();
			int si = 0;
			int ni = 0;

			synsetQueue.Enqueue(WordnameSynsetsMap["entity"][0]);

			while ( synsetQueue.Count > 0 ) {
				Synset synset = synsetQueue.Dequeue();

				synset.UniqueName = CalcUniqueSynsetName(synset, synsetNameMap, out bool usedNum);
				//string suffix = "__wns"+SynsetPosAbbrevs[pSynset.PartOfSpeechId];

				if ( usedNum ) {
					Console.WriteLine(si+": "+synset.UniqueName+"  ...  "+synset.ToDebugString());
					string origName = synset.UniqueName.Substring(0, synset.UniqueName.Length-1);
					Synset origSynset = synsetNameMap[origName];
					Console.WriteLine("    ORIGINAL: ["+ni+"] "+origSynset.ToDebugString());
					ni++;
				}

				si++;

				synsetNameMap.Add(synset.UniqueName, synset);
				synsetHitMap.Add(synset.Id);

				if ( ni >= 10 ) {
					break;
				}

				List<Synset> children = GetHypernymSynsets(synset, false);

				foreach ( Synset child in children ) {
					synsetQueue.Enqueue(child);
				}
			}
		}

		/*--------------------------------------------------------------------------------------------* /
		private static string CalcUniqueSynsetName(Synset pSynset, 
										Dictionary<string, Synset> pNameMap, out bool pUsedNumbering) {
			IList<Word> words = pSynset.WordList;

			string name = string.Join("_", words.Select(w => w.Name))+"__";

			IEnumerator<Word> hyperWords = GetHypernymSynsets(pSynset)
				.SelectMany(s => s.WordList).GetEnumerator();
			IEnumerator<Word> domainWords = GetDomainSynsets(pSynset)
				.SelectMany(s => s.WordList).GetEnumerator();
			IEnumerator<Word> attribWords = GetAttributeSynsets(pSynset)
				.SelectMany(s => s.WordList).GetEnumerator();

			int dedupI = 0;

			while ( pNameMap.ContainsKey(name) ) {
				Word addWord = null;

				if ( hyperWords.MoveNext() ) {
					addWord = hyperWords.Current;
				}
				else if ( domainWords.MoveNext() ) {
					addWord = domainWords.Current;
				}
				else if ( attribWords.MoveNext() ) {
					addWord = attribWords.Current;
				}

				if ( addWord != null ) {
					name += "_"+addWord.Name;
					continue;
				}

				name += (++dedupI+1);
			}

			hyperWords.Dispose();
			domainWords.Dispose();
			attribWords.Dispose();

			pUsedNumbering = (dedupI > 0);
			return name;
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void CalcUniqueSynsetNames() {
			Console.WriteLine("CalcUniqueSynsetNames...");

			var conflictMap = new Dictionary<string, List<SynsetName>>();
			int counter = 0;

			foreach ( Synset synset in SynsetList ) {
				var name = new SynsetName(synset);
				string key = name.PartOfSpeechText+":"+name.MembersText; //+"__"+name.HypernymsText;

				if ( !conflictMap.ContainsKey(key) ) {
					conflictMap.Add(key, new List<SynsetName>());
				}

				conflictMap[key].Add(name);
			}

			foreach ( KeyValuePair<string, List<SynsetName>> conflictPair in conflictMap ) {
				if ( conflictPair.Value.Count <= 1 ) {
					continue;
				}

				var subconflictMap = new HashSet<string>();
				bool hasSubconflict = false;

				foreach ( SynsetName name in conflictPair.Value ) {
					string subtext = 
						name.HypernymsText+"+"+
						name.HolonnymsText+"+"+
						name.EntailmentsText+"+"+
						name.DomainsText+"+"+
						name.CausesText+"+"+
						name.VerbGroupsText+"+"+
						name.AttributesText+"+"+
						name.SimilarText+"+"+
						//name.AlsoSeeText+"+"+
						name.LexPertainsText+"+"+
						name.LexRelatesText+"+"+
						name.LexDerivesText+"+"+
						name.GlossMajorsText+"+"+
						name.GlossMinorsText;

					if ( !subconflictMap.Add(subtext) ) {
						hasSubconflict = true;
					}
				}

				if ( !hasSubconflict ) {
					continue;
				}

				if ( ++counter >= 1000 ) {
					break;
				}

				Console.WriteLine(conflictPair.Key+" ["+counter+"]");

				foreach ( SynsetName name in conflictPair.Value ) {
					Console.WriteLine("    *"+
						(name.HypernymsText 	!= null ? " [H] "+name.HypernymsText : "")+
						(name.HolonnymsText		!= null ? " [O] "+name.HolonnymsText : "")+
						(name.EntailmentsText 	!= null ? " [E] "+name.EntailmentsText : "")+
						(name.DomainsText		!= null ? " [D] "+name.DomainsText : "")+
						(name.CausesText		!= null ? " [C] "+name.CausesText : "")+
						(name.VerbGroupsText	!= null ? " [V] "+name.VerbGroupsText : "")+
						(name.AttributesText	!= null ? " [A] "+name.AttributesText : "")+
						(name.SimilarText		!= null ? " [S] "+name.SimilarText : "")+
						//(name.AlsoSeeText		!= null ? " [L] "+name.AlsoSeeText : "")+
						(name.LexPertainsText	!= null ? " [LP] "+name.LexPertainsText : "")+
						(name.LexRelatesText	!= null ? " [LR] "+name.LexRelatesText : "")+
						(name.LexDerivesText	!= null ? " [LD] "+name.LexDerivesText : "")+
						" [GM] "+name.GlossMajorsText+" / "+
							name.GlossMinorsText+" { "+name.GlossNoExamplesText+" }"
					);
				}
			}
		}

		/*--------------------------------------------------------------------------------------------* /
		private static void PrintWordSynsets(int pIndex) {
			Console.WriteLine("PrintWordSynsets...");

			var wsQueue = new Queue<WordnameSynsets>();
			wsQueue.Enqueue(WordameSynsetList[pIndex]);

			var wordnameMap = new HashSet<string>();
			wordnameMap.Add(wsQueue.Peek().Name);

			int countdown = 100;

			while ( wsQueue.Count > 0 ) {
				WordnameSynsets ws = wsQueue.Dequeue();
				Console.WriteLine("\n"+ws.Name.ToUpper());

				foreach ( Synset synset in ws.Synsets ) {
					var sn = new SynsetName(synset);
					Console.WriteLine(sn);

					foreach ( Word word in synset.WordList ) {
						if ( wordnameMap.Contains(word.Name) ) {
							continue;
						}

						wordnameMap.Add(word.Name);

						wsQueue.Enqueue(new WordnameSynsets {
							Name = word.Name,
							Synsets = WordnameSynsetsMap[word.Name]
						});
					}
				}

				if ( --countdown < 0 ) {
					break;
				}
			}
		}*/

		private class Node {

			public Node ParentNode { get; private set; }
			public string Name { get; private set; }
			public int Depth { get; private set; }
			public List<SynsetName> SynNames { get; private set; }
			public List<Node> ChildNodes { get; private set; }

			public Node(Node pParentNode, string pName, int pDepth) {
				ParentNode = pParentNode;
				Name = pName;
				Depth = pDepth;
				SynNames = new List<SynsetName>();
				ChildNodes = new List<Node>();
			}

			public void ConvertSynNamesIntoChildNodes() {
				if ( SynNames.Count == 0 ) {
					return;
				}

				if ( SynNames.Count == 1 ) {
					SynNames[0].Synset.UniqueName = ToUniqueString();
					return;
				}

				var childMap = new Dictionary<string, Node>();

				for ( int i = SynNames.Count-1 ; i >= 0 ; i-- ) {
					SynsetName synName = SynNames[i];

					if ( Depth >= synName.AllNames.Count ) {
						continue;
					}

					string childName = synName.AllNames[Depth].ToLower();

					if ( !childMap.ContainsKey(childName) ) {
						var childNode = new Node(this, childName, Depth+1);
						childMap.Add(childName, childNode);
						ChildNodes.Add(childNode);
					}

					childMap[childName].SynNames.Add(synName);
					SynNames.RemoveAt(i);
				}

				foreach ( Node childNode in ChildNodes ) {
					childNode.ConvertSynNamesIntoChildNodes();
				}
			}

			public bool IsSilent() {
				return (ChildNodes.Count == 1 && ParentNode?.ChildNodes.Count == 1);
			}

			public override string ToString() {
				return $"Node[{Depth}:{Name}:{SynNames.Count}:{ChildNodes.Count}]";
			}

			public string ToUniqueString() {
				var sb = new StringBuilder();
				sb.Append(Name);

				Node node = ParentNode;

				while ( node != null ) {
					if ( !node.IsSilent() ) {
						sb.Insert(0, '|'); //(node.Depth < 2 ? '.' : '_'));
						sb.Insert(0, node.Name);
					}

					node = node.ParentNode;
				}

				return sb.ToString();
			}

			public string ToTreeString() {
				var sb = new StringBuilder();

				for ( int i = 0 ; i < Depth ; i++ ) {
					sb.Append("|   ");
				}

				if ( ChildNodes.Count == 1 ) {
					sb.Append('(');
					sb.Append(Name);
					sb.Append(')');
				}
				else {
					sb.Append(Name);
				}

				if ( SynNames.Count > 0 ) {
					sb.Append(' ');
					sb.Append('*', SynNames.Count);
					sb.Append(' ');
					sb.Append(ToUniqueString());
				}

				sb.Append('\n');

				foreach ( Node childNode in ChildNodes ) {
					sb.Append(childNode.ToTreeString());
				}

				return sb.ToString();
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void PerformSynsetUniqueTree() {
			Console.WriteLine("PerformSynsetUniqueTree...");

			var rootNode = new Node(null, "wn", 0);
			rootNode.SynNames.AddRange(SynsetList.Select(s => new SynsetName(s)));
			rootNode.ConvertSynNamesIntoChildNodes();

			List<string> uniqueSynsetNames = SynsetList.Select(s => s.UniqueName).ToList();
			uniqueSynsetNames.Sort();

			var uniqueCheckMap = new HashSet<string>();

			const string folder = @"D:\Work\AEI\Notable\Docs\";
			const string pathTree = folder+"synset-tree.txt";
			const string pathNames = folder+"synset-names.txt";

			using ( FileStream fs = File.Open(pathTree, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					fsw.Write(rootNode.ToTreeString());
				}
			}

			using ( FileStream fs = File.Open(pathNames, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					foreach ( string uniqueSynsetName in uniqueSynsetNames ) {
						fsw.Write(uniqueSynsetName);

						if ( !uniqueCheckMap.Add(uniqueSynsetName) ) {
							fsw.Write(" <DUPLICATE>");
						}

						fsw.Write('\n');
					}
				}
			}
		}

	}

}
