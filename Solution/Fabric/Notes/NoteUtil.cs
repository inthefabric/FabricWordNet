using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Wordnet;
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

		private const string SynsetSortKey = "wn_syn";
		private const string SemanticSortKey = "wn_sem";
		private static readonly string[] SynsetPosAbbrevs = { "", "n", "v", "adj", "adv" };

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
		private static IList<Word> WordList;
		private static Dictionary<int, Word> WordMap;
		private static Dictionary<int, List<Word>> SynsetWordsMap;
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
				BuildWordNet.SetDbStateBeforeBatchInsert(sess);
				GetSynsets(sess);
				GetWords(sess);
				GetSemantics(sess);
				BuildWordNet.SetDbStateAfterBatchInsert(sess);
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

			foreach ( Synset synset in SynsetList ) {
				string[] split = synset.SsId.Split(':');

				synset.SsId = "_"+SynsetPosAbbrevs[synset.PartOfSpeechId]+split[1];
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

			WordList = pSess.QueryOver<Word>().List();

			WordMap = new Dictionary<int, Word>();
			SynsetWordsMap = new Dictionary<int, List<Word>>();

			foreach ( Word word in WordList ) {
				WordMap.Add(word.Id, word);

				if ( SynsetWordsMap.ContainsKey(word.Synset.Id) ) {
					SynsetWordsMap[word.Synset.Id].Add(word);
				}
				else {
					SynsetWordsMap.Add(word.Synset.Id, new List<Word> { word });
				}
			}

			Console.WriteLine("GetWords complete: "+
				$"{WordList.Count} results, {timer.Elapsed.TotalSeconds} sec");
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
		private static string WordNameToTag(string pWordName) {
			string name = pWordName;
			name = Regex.Replace(name, @"\(.+\)", ""); //remove endings like "galore(ip)"
			//if ( pWordName != name ) { Console.WriteLine("WN "+pWordName+" => "+name); }
			name = Regex.Replace(name, @"[\s_]+", "-");
			name = Regex.Replace(name, @"[^\w\-]+", "");
			return (name.Length < 3 ? null : name);
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

					fsw.Write( "\n"+ToNoteJson(NextNoteId++, $"${SynsetSortKey}_"+
						$"{SynsetPosAbbrevs[(int)POS.Noun]}-[{wn3} Noun Synset]"));
					fsw.Write(",\n"+ToNoteJson(NextNoteId++, $"${SynsetSortKey}_"+
						$"{SynsetPosAbbrevs[(int)POS.Verb]}-[{wn3} Verb Synset]"));
					fsw.Write(",\n"+ToNoteJson(NextNoteId++, $"${SynsetSortKey}_"+
						$"{SynsetPosAbbrevs[(int)POS.Adjective]}-[{wn3} Adjective Synset]"));
					fsw.Write(",\n"+ToNoteJson(NextNoteId++, $"${SynsetSortKey}_"+
						$"{SynsetPosAbbrevs[(int)POS.Adverb]}-[{wn3} Adverb Synset]"));

					foreach ( SsRelInfo ssr in SsRelList ) {
						fsw.Write(",\n"+ToNoteJson(NextNoteId++, $"${SemanticSortKey}_"+
							$"{ssr.Abbrev}-[{wn3} {ssr.Label}]"));
					}

					Action<Note> writeNoteFunc = ((n) => {
						fsw.Write(",\n"+ToNoteJson(NextNoteId++, n.Text));

						if ( NextNoteId%10000 == 0 ) {
							Console.WriteLine($" ... at note: {NextNoteId}");
							fsw.Flush();
						}
					});

					WriteSynsetNotes(writeNoteFunc);
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
		private static string ToSynsetWordString(this Synset pSynset) {
			var text = new StringBuilder();
			List<Word> words = SynsetWordsMap[pSynset.Id];
			int wordCount = words.Count;

			if ( wordCount > 1 ) {
				text.Append(" {");
			}

			for ( int i = 0 ; i < wordCount ; i++ ) {
				string name = words[i].Name;
				string tag = WordNameToTag(name);

				if ( tag == null ) {
					text.Append(" ");
					text.Append(name);
				}
				else {
					text.Append(" #");
					text.Append(tag);
				}

				if ( i < wordCount-1 ) {
					text.Append(" |");
				}
			}

			if ( wordCount > 1 ) {
				text.Append(" }");
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
			text.Append(ToSynsetWordString(pSynset).Substring(1));

			text.Append(" %as #");
			text.Append(PartOfSpeechTextMap[(POS)pSynset.PartOfSpeechId]);
			text.Append(" >means {");

			int glossSplitIndex = pSynset.Gloss.IndexOf("; \"");
			string glossMain = pSynset.Gloss;
			string glossExamples = null;

			if ( glossSplitIndex != -1 ) {
				glossExamples = glossMain.Substring(glossSplitIndex+2);
				glossMain = glossMain.Substring(0, glossSplitIndex);
			}

			text.Append(ModifyGlossForNote(glossMain));
			text.Append(" }");
			text.Append(" //");

			if ( glossExamples != null ) {
				text.Append(" for ^example: {");
				text.Append(ModifyGlossForNote(glossExamples, false));
				text.Append(" }");
			}

			text.Append(" $");
			text.Append(SynsetSortKey);
			text.Append(pSynset.SsId);

			return Note.New(NoteType.SynsetMeansGloss, text.ToString());
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

			text.Append(ToSynsetWordString(fromSyn).Substring(1));
			text.Append(" %as #");
			text.Append(PartOfSpeechTextMap[(POS)fromSyn.PartOfSpeechId]);

			text.Append(" ");
			text.Append(info.Action);

			text.Append(ToSynsetWordString(toSyn));
			text.Append(" %as #");
			text.Append(PartOfSpeechTextMap[(POS)toSyn.PartOfSpeechId]);

			text.Append(" //");
			text.Append(" $");
			text.Append(SemanticSortKey);
			text.Append("_");
			text.Append(info.Abbrev);
			text.Append(pSemantic.Id);

			text.Append(" from $");
			text.Append(SynsetSortKey);
			text.Append(fromSyn.SsId);

			text.Append(" to $");
			text.Append(SynsetSortKey);
			text.Append(toSyn.SsId);

			return Note.New(NoteType.Semantic, text.ToString());
		}

	}

}
