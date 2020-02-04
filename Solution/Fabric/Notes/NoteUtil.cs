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
	public static class NoteUtil {

		private struct SsRelInfo {
			public SynSetRelation Rel;
			public bool IsBiDir;
			public string Action;
			public string Abbrev;
			public string Label;
			public SynSetRelation? RelOpp;

			public SsRelInfo(SynSetRelation pRel, bool pIsBiDir, string pAction,
														string pAbbrev,	SynSetRelation? pRelOpp=null) {
				Rel = pRel;
				IsBiDir = pIsBiDir;
				Action = ">"+pAction+(IsBiDir ? "<" : "");
				Abbrev = pAbbrev;
				Label = "Semantic "+pRel;
				RelOpp = pRelOpp;
			}
		}

		private const string SynsetSortKey = "wns";
		private const string WordSortKey = "wnw";
		private const string SemanticSortKey = "wnm";
		private const string LexicalSortKey = "wnl";
		private static readonly string[] SynsetPosAbbrevs = { "", "n", "v", "j", "b" };
		//private static readonly string[] SynsetPosNames = { "", "Noun", "Verb", "Adjective", "Adverb"};

		private static SsRelInfo[] SsRelList = {
			new SsRelInfo(SynSetRelation.Hypernym,
				false, "is-type-of", "h", SynSetRelation.Hyponym),
			new SsRelInfo(SynSetRelation.InstanceHypernym,
				false, "is-instance-of", "ih", SynSetRelation.InstanceHyponym),
			new SsRelInfo(SynSetRelation.MemberHolonym,
				false, "is-member-of", "mh", SynSetRelation.MemberMeronym),
			new SsRelInfo(SynSetRelation.PartHolonym,
				false, "is-part-of", "ph", SynSetRelation.PartMeronym),
			new SsRelInfo(SynSetRelation.SubstanceHolonym,
				false, "is-made-of", "sh", SynSetRelation.SubstanceMeronym),
			new SsRelInfo(SynSetRelation.TopicDomain,
				false, "is-topic-of", "td", SynSetRelation.TopicDomainMember),
			new SsRelInfo(SynSetRelation.UsageDomain,
				false, "is-usage-of", "ud", SynSetRelation.UsageDomainMember),
			new SsRelInfo(SynSetRelation.RegionDomain,
				false, "is-region-of", "rd", SynSetRelation.RegionDomainMember),

			new SsRelInfo(SynSetRelation.VerbGroup, false, "is-subset-of", "v"),
			new SsRelInfo(SynSetRelation.Entailment, false, "requires", "e"),
			new SsRelInfo(SynSetRelation.Cause, false, "causes", "c"), //TODO: directionality?

			new SsRelInfo(SynSetRelation.SimilarTo, true, "is-similar-to", "s"),
			new SsRelInfo(SynSetRelation.AlsoSee, true, "is-related-to", "as"),
			new SsRelInfo(SynSetRelation.Attribute, true, "is-attribute-of", "a") //NEW

			//not present in database
			//SynSetRelation.None
			//SynSetRelation.Antonym
			//SynSetRelation.DerivationallyRelated
			//SynSetRelation.DerivedFromAdjective
			//SynSetRelation.ParticipleOfVerb
			//SynSetRelation.Pertainym
		};

		private static Dictionary<SynSetRelation, SsRelInfo> SsRelMap =
			SsRelList.ToList().ToDictionary(x => x.Rel);

		private static Dictionary<POS, string> PartOfSpeechTextMap;
		private static IList<Synset> SynsetList;
		private static Dictionary<int, Synset> SynsetMap;
		private static HashSet<string> SynsetHashCheck;
		private static Dictionary<int, string> SynsetHash;
		private static IList<Word> WordList;
		private static Dictionary<int, List<Word>> SynsetWordsMap;
		private static Dictionary<string, List<Synset>> WordtagSynsetsMap;
		private static HashSet<string> TagMap;
		private static IList<Semantic> SemanticList;
		private static int NextNoteId = 1000000;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void GenerateNotes() {
			Console.WriteLine("GenerateNotes...\n");
			Stopwatch timer = Stopwatch.StartNew();
			DbBuilder.UpdateSchema();

			BuildPosMap();

			using ( ISession sess = new SessionProvider().OpenSession() ) {
				GetSynsets(sess);
				GetWords(sess);
				CalcSynsetHashes();
				GetSemantics(sess);
			}

			BuildTagMap();
			WriteAllNotes();

			Console.WriteLine($"\nGenerateNotes complete: {timer.Elapsed.TotalSeconds} sec");
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void BuildPosMap() {
			PartOfSpeechTextMap = new Dictionary<POS, string>();

			foreach ( POS pos in Enum.GetValues(typeof(POS)) ) {
				PartOfSpeechTextMap.Add(pos, pos.ToString().ToLower());
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void GetSynsets(ISession pSess) {
			Console.WriteLine("GetSynsets... (wait ~15 sec)");
			var timer = Stopwatch.StartNew();
			var ssidMap = new HashSet<int>();

			SynsetList = pSess.QueryOver<Synset>().List();
			SynsetMap = new Dictionary<int, Synset>();
			SynsetHashCheck = new HashSet<string>();
			SynsetHash = new Dictionary<int, string>();

			foreach ( Synset synset in SynsetList ) {
				string[] split = synset.SsId.Split(':');

				synset.SsId = SynsetPosAbbrevs[synset.PartOfSpeechId]+split[1];
				synset.SortValue = int.Parse(split[1])+(synset.PartOfSpeechId-1)*100000000;

				if ( ssidMap.Contains(synset.SortValue) ) {
					Console.WriteLine("DUP: "+synset.SsId+" / "+synset.SortValue);
				}

				ssidMap.Add(synset.SortValue);
				SynsetMap.Add(synset.Id, synset);
			}

			Console.WriteLine("GetSynsets complete: "+
				$"{SynsetList.Count} results, {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void GetWords(ISession pSess) {
			Console.WriteLine("GetWords... (wait ~20 sec)");
			var timer = Stopwatch.StartNew();
			const int keyLen = 5;

			WordList = pSess.QueryOver<Word>().List();

			SynsetWordsMap = new Dictionary<int, List<Word>>();
			WordtagSynsetsMap = new Dictionary<string, List<Synset>>();

			foreach ( Word word in WordList ) {
				string wordTag = WordNameToTag(word.Name, true);

				if ( !SynsetWordsMap.ContainsKey(word.Synset.Id) ) {
					SynsetWordsMap.Add(word.Synset.Id, new List<Word>());
				}

				if ( !WordtagSynsetsMap.ContainsKey(wordTag) ) {
					WordtagSynsetsMap.Add(wordTag, new List<Synset>());
				}

				SynsetWordsMap[word.Synset.Id].Add(word);
				WordtagSynsetsMap[wordTag].Add(SynsetMap[word.Synset.Id]);
			}

			foreach ( KeyValuePair<int, List<Word>> pair in SynsetWordsMap ) {
				pair.Value.Sort((a,b) => {
					string tagA = WordNameToTag(a.Name, true);
					string tagB = WordNameToTag(b.Name, true);
					int countA = WordtagSynsetsMap[tagA].Count;
					int countB = WordtagSynsetsMap[tagB].Count;

					if ( countA == countB ) {
						return a.Name.Length-b.Name.Length;
					}

					return countA-countB;
				});
			}

			Console.WriteLine("GetWords complete: "+
				$"{WordList.Count} results, {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void CalcSynsetHashes() {
			const int keyLen = 5;

			foreach ( Synset syn in SynsetList ) {
				List<Word> words = SynsetWordsMap[syn.Id];
				string key = "";

				for ( int i = 0 ; i < words.Count ; i++ ) {
					string tag = WordNameToTag(words[i].Name, true);
					int tagI = 0;

					while ( key.Length < keyLen && tagI < tag.Length ) {
						key += tag[tagI++];
					}

					if ( key.Length >= keyLen ) {
						break;
					}
				}

				int unique = 0;
				string hash = key+unique;

				while ( SynsetHashCheck.Contains(hash) ) {
					if ( unique > 800 ) {
						Console.WriteLine($"--- dup ss key: {key} {hash}");
					}

					unique++;
					hash = key+unique;
				}

				SynsetHashCheck.Add(hash);
				SynsetHash.Add(syn.Id, hash);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void GetSemantics(ISession pSess) {
			Console.WriteLine("GetSemantics... (wait ~24 sec)");
			Stopwatch timer = Stopwatch.StartNew();
			SemanticList = pSess.QueryOver<Semantic>().List();
			Console.WriteLine($"GetSemantics complete: {SemanticList.Count} results, "+
				$"{timer.Elapsed.TotalSeconds} sec");
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static string WordNameToTag(string pWordName, bool pForceTag=false) {
			string name = pWordName.ToLower();
			name = Regex.Replace(name, @"\(.+\)", ""); //remove endings like "galore(ip)"
			//if ( pWordName != name ) { Console.WriteLine("WN "+pWordName+" => "+name); }
			name = Regex.Replace(name, @"[\s_]+", "-");
			name = Regex.Replace(name, @"[^\w\-]+", "");
			return (name.Length < 3 && !pForceTag ? null : name);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void BuildTagMap() {
			Console.WriteLine("BuildTagMap... (wait ~2 sec)");
			var timer = Stopwatch.StartNew();

			TagMap = new HashSet<string>();

			foreach ( Synset syn in SynsetList ) {
				List<Word> words = SynsetWordsMap[syn.Id];

				foreach ( Word word in words ) {
					string tag = WordNameToTag(word.Name);

					if ( tag != null ) {
						TagMap.Add(tag.ToLower());
					}
				}
			}

			Console.WriteLine("BuildTagMap complete: "+
				$"{TagMap.Count} results, {timer.Elapsed.TotalSeconds} sec");
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void WriteAllNotes() {
			Console.WriteLine($"\nWriteAllNotes...\n");
			const string path = "/Users/zachkinstner/Downloads/sot/wn2.json";
			const string wn3 = "WordNet 3.1:";
			Stopwatch timer = Stopwatch.StartNew();

			using ( FileStream fs = File.Open(path, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					fsw.Write("{\"notes\":[");
					fsw.Flush();

					for ( int i = 1 ; i <= 4 ; i++ ) {
						string posName = PartOfSpeechTextMap[(POS)i];
						posName = posName.ToUpper()[0]+posName.Substring(1);

						fsw.Write(
							(i == 1 ? "" : ",")+"\n"+
							ToNoteJson(NextNoteId++,
							$"${SynsetSortKey}"+
							$"{SynsetPosAbbrevs[i]}-[{wn3} {posName} Synset]")
						);

						/*fsw.Write(
							",\n"+
							ToNoteJson(NextNoteId++,
							$"${WordSortKey}"+
							$"{SynsetPosAbbrevs[i]}-[{wn3} {posName} Word]")
						);*/
					}

					/*foreach ( SsRelInfo ssr in SsRelList ) {
						fsw.Write(",\n"+ToNoteJson(NextNoteId++, $"${SemanticSortKey}"+
							$"{ssr.Abbrev}-[{wn3} {ssr.Label}]"));
					}*/

					Action<Note> writeNoteFunc = ((n) => {
						fsw.Write(",\n"+ToNoteJson(NextNoteId++, n.Text));

						if ( NextNoteId%10000 == 0 ) {
							Console.WriteLine($" ...at note: {NextNoteId}");
							fsw.Flush();
						}
					});

					WriteSynsetNotes(writeNoteFunc);
					WriteWordNotes(writeNoteFunc);
					WriteSemanticNotes(writeNoteFunc);

					fsw.Write("\n]}");
					fsw.Flush();
				}
			}

			Console.WriteLine($"\nWriteAllNotes complete: {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string ToNoteJson(int pNoteId, string pText) {
			const string q = "\"";

			return
				"{"+
					$"{q}id{q}:{pNoteId},"+
					$"{q}t{q}:{q}{pText.Replace(q, "\\\"")}{q}"+
				"}";
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void WriteSynsetNotes(Action<Note> pWriteNoteFunc) {
			Console.WriteLine("WriteSynsetNotes... (wait ~30 sec)");
			Stopwatch timer = Stopwatch.StartNew();

			foreach ( Synset syn in SynsetList ) {
				pWriteNoteFunc(syn.ToNote());
			}

			Console.WriteLine($"WriteSynsetNotes complete: {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string ToSynsetString(this Synset pSynset) {
			var text = new StringBuilder();
			text.Append(" #");
			text.Append(SynsetHash[pSynset.Id]);
			return text.ToString();
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string ToSynsetWordsString(this Synset pSynset) {
			List<Word> words = SynsetWordsMap[pSynset.Id];
			int wordCount = words.Count;
			var text = new StringBuilder();

			for ( int i = 0 ; i < wordCount ; i++ ) {
				text.Append(" ");
				text.Append(words[i].Name);

				if ( i < wordCount-1 ) {
					text.Append(" /");
				}
 			}

			return text.ToString();
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string ModifyGlossForNote(string pGloss, bool pAllowTags=true) {
			string[] tokens = Regex.Split(pGloss, @"\s");
			var text = new StringBuilder();

			foreach ( string rawGlossToken in tokens ) {
				string glossToken = rawGlossToken;

				glossToken = Regex.Replace(glossToken,
					@"\+\+", " ++"); //fix "C++" scenario

				glossToken = Regex.Replace(glossToken,
					@"(^|[^\w])([\+\^])(\w)", "$1$2 $3"); //fix "+1" and "x)^2" scenarios

				/*if ( glossToken != rawGlossToken ) {
					Console.WriteLine("FIX: "+rawGlossToken+" => "+glossToken);
				}*/

				Match match = (pAllowTags ? Regex.Match(glossToken, @"[\w\-]+") : null);
				text.Append(" ");

				if ( match?.Success == true && TagMap.Contains(match.Value.ToLower()) ) {
					text.Append(glossToken.Insert(match.Index, "#"));
				}
				else {
					text.Append(glossToken);
				}
			}

			return text.ToString();
		}

		/*--------------------------------------------------------------------------------------------*/
		private static Note ToNote(this Synset pSynset) {
			var text = new StringBuilder();

			int glossSplitIndex = pSynset.Gloss.IndexOf("; \"");
			string glossMain = pSynset.Gloss;
			//string glossExamples = null;

			if ( glossSplitIndex != -1 ) {
				//glossExamples = glossMain.Substring(glossSplitIndex+2);
				glossMain = glossMain.Substring(0, glossSplitIndex);
			}

			text.Append(ToSynsetString(pSynset).Substring(1));
			text.Append("[(");
			text.Append(PartOfSpeechTextMap[(POS)pSynset.PartOfSpeechId]);
			text.Append(")");
			text.Append(ToSynsetWordsString(pSynset));
			text.Append(":");
			text.Append(ModifyGlossForNote(glossMain, false));
			text.Append("]");

			text.Append(" %as #");
			text.Append(PartOfSpeechTextMap[(POS)pSynset.PartOfSpeechId]);

			text.Append(" //");
			text.Append(" $");
			text.Append(SynsetSortKey);
			text.Append(pSynset.SsId);

			/*if ( glossExamples != null ) {
				text.Append(" ^example {");
				text.Append(ModifyGlossForNote(glossExamples, false));
				text.Append(" }");
			}*/

			return Note.New(NoteType.SynsetMeansGloss, text.ToString());
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void WriteWordNotes(Action<Note> pWriteNoteFunc) {
			Console.WriteLine("WriteWordNotes... (wait ~12 sec)");
			Stopwatch timer = Stopwatch.StartNew();

			foreach ( Word word in WordList ) {
				string wordTag = WordNameToTag(word.Name);

				if ( wordTag == null ) {
					continue;
				}

				List<Synset> synsets = WordtagSynsetsMap[wordTag];

				foreach ( Synset syn in synsets ) {
					Note note = ToNote(word, wordTag, syn);

					if ( note != null ) {
						pWriteNoteFunc(note);
					}
				}
			}

			Console.WriteLine($"WriteWordNotes complete: {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static Note ToNote(this Word pWord, string pWordTag, Synset pSynset) {
			var text = new StringBuilder();

			text.Append("#");
			text.Append(pWordTag);
			//text.Append(" %as #");
			//text.Append(PartOfSpeechTextMap[(POS)pSynset.PartOfSpeechId]);

			text.Append(" >means");
			text.Append(ToSynsetString(pSynset));

			/*text.Append(" //");
			text.Append(" $");
			text.Append(WordSortKey);
			text.Append(SynsetPosAbbrevs[pSynset.PartOfSpeechId]);
			text.Append(pWord.Id);*/

			return Note.New(NoteType.Semantic, text.ToString());
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void WriteSemanticNotes(Action<Note> pWriteNoteFunc) {
			Console.WriteLine("WriteSemanticNotes... (wait ~10 sec)");
			Stopwatch timer = Stopwatch.StartNew();

			foreach ( Semantic sem in SemanticList ) {
				Note note = ToNote(sem);

				if ( note != null ) {
					pWriteNoteFunc(note);
				}
			}

			Console.WriteLine($"WriteSemanticNotes complete: {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static Note ToNote(this Semantic pSemantic) {
			SynSetRelation rel = (SynSetRelation)pSemantic.RelationId;

			if ( !SsRelMap.ContainsKey(rel) ) {
				return null;
			}

			SsRelInfo info = SsRelMap[rel];

			var text = new StringBuilder();
			Synset fromSyn = SynsetMap[pSemantic.Synset.Id];
			Synset toSyn = SynsetMap[pSemantic.TargetSynset.Id];

			text.Append(ToSynsetString(fromSyn).Substring(1));
			//text.Append(" %as #");
			//text.Append(PartOfSpeechTextMap[(POS)fromSyn.PartOfSpeechId]);

			text.Append(" ");
			text.Append(info.Action);

			text.Append(ToSynsetString(toSyn));
			//text.Append(" %as #");
			//text.Append(PartOfSpeechTextMap[(POS)toSyn.PartOfSpeechId]);

			/*text.Append(" //");
			text.Append(" $");
			text.Append(SemanticSortKey);
			text.Append(info.Abbrev);
			text.Append(pSemantic.Id);*/

			/*text.Append(" from $");
			text.Append(SynsetSortKey);
			text.Append(fromSyn.SsId);

			text.Append(" to $");
			text.Append(SynsetSortKey);
			text.Append(toSyn.SsId);*/

			return Note.New(NoteType.Semantic, text.ToString());
		}

	}

}
