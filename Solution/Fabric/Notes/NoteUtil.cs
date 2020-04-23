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
				Action = ">"+pAction; //+(IsBiDir ? "<" : "");
				Abbrev = pAbbrev;
				Label = "Semantic "+pRel;
				RelOpp = pRelOpp;
			}
		}

		private const string SynsetSortKey = "wns";
		//private const string WordSortKey = "wnw";
		//private const string SemanticSortKey = "wnm";
		//private const string LexicalSortKey = "wnl";
		private static readonly string[] SynsetPosAbbrevs = { "", "n", "v", "a", "b" };
		//private static readonly string[] SynsetPosNames = { "", "Noun", "Verb", "Adjective", "Adverb"};

		private static SsRelInfo[] SsRelList = {
			//new SsRelInfo(SynSetRelation.None, false, "is-NONE-of", "n"),

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
			new SsRelInfo(SynSetRelation.Cause, false, "causes", "c"),
			new SsRelInfo(SynSetRelation.Pertainym, false, "pertains-to", "pe"),
			new SsRelInfo(SynSetRelation.DerivedFromAdjective, false, "derives-from", "df"),
			new SsRelInfo(SynSetRelation.ParticipleOfVerb, false, "is-participle-of", "pa"),

			new SsRelInfo(SynSetRelation.SimilarTo, true, "is-similar-to", "s"),
			new SsRelInfo(SynSetRelation.AlsoSee, true, "relates-also-to", "as"),
			new SsRelInfo(SynSetRelation.Attribute, true, "is-attribute-of", "a"),
			new SsRelInfo(SynSetRelation.Antonym, true, "is-opposite-of", "o"),
			new SsRelInfo(SynSetRelation.DerivationallyRelated, true, "relates-to", "dr")
		};

		private static Dictionary<SynSetRelation, SsRelInfo> SsRelMap =
			SsRelList.ToList().ToDictionary(x => x.Rel);

		private static Dictionary<POS, string> PartOfSpeechTextMap;
		private static List<Synset> SynsetList;
		private static Dictionary<int, Synset> SynsetMap;
		private static HashSet<string> SynsetHashCheck;
		private static Dictionary<int, string> SynsetHash;
		private static List<Word> WordList;
		private static Dictionary<int, Word> WordMap;
		private static Dictionary<int, List<Word>> SynsetWordsMap;
		private static Dictionary<string, List<Synset>> WordtagSynsetsMap;
		private static HashSet<string> TagMap;
		private static IList<Semantic> SemanticList;
		private static IList<Lexical> LexicalList;
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
				GetLexicals(sess);
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

			SynsetList = pSess.QueryOver<Synset>().List().ToList();
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

			SynsetList.Sort((a,b) => a.SortValue-b.SortValue);

			Console.WriteLine("GetSynsets complete: "+
				$"{SynsetList.Count} results, {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void GetWords(ISession pSess) {
			Console.WriteLine("GetWords... (wait ~26 sec)");
			var timer = Stopwatch.StartNew();
			const int keyLen = 5;

			WordList = pSess.QueryOver<Word>().List().ToList();
			WordMap = new Dictionary<int, Word>();
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

				WordMap.Add(word.Id, word);
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
			Console.WriteLine("GetSemantics... (wait ~26 sec)");
			Stopwatch timer = Stopwatch.StartNew();
			SemanticList = pSess.QueryOver<Semantic>().List();
			Console.WriteLine($"GetSemantics complete: {SemanticList.Count} results, "+
				$"{timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void GetLexicals(ISession pSess) {
			Console.WriteLine("GetLexicals... (wait ~11 sec)");
			Stopwatch timer = Stopwatch.StartNew();
			LexicalList = pSess.QueryOver<Lexical>().List();
			Console.WriteLine($"GetLexicals complete: {LexicalList.Count} results, "+
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
			Console.WriteLine("BuildTagMap... (wait ~3 sec)");
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
			const string folder = "/Users/zachkinstner/Work/Notable/Notable2/Data/";
			const string pathSynsets = folder+"wordnet-synsets.txt";
			const string pathSemantics = folder+"wordnet-semantics.txt";
			const string pathLexicals = folder+"wordnet-lexicals.txt";
			const string headerName = "//// name: ";
			const string headerVersion = "//// version: ";
			const string headerSource = "//// source: ";
			const string wn3 = "WordNet 3.1:";
			const string version = "1";
			const string source = "https://wordnet.princeton.edu/download/current-version";
			const string lineSep = "\n\n";
			Stopwatch timer = Stopwatch.StartNew();

			Action<StreamWriter> outAndFlush = ((fsw) => {
				if ( ++NextNoteId%10000 == 0 ) {
					Console.WriteLine($" ...at note: {NextNoteId}");
					fsw.Flush();
				}
			});

			using ( FileStream fs = File.Open(pathSynsets, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					fsw.Write(  $"{headerName}{wn3} Synsets");
					fsw.Write($"\n{headerSource}{source}");
					fsw.Write($"\n{headerVersion}{version}");
					fsw.Flush();

					for ( int i = 1 ; i <= 4 ; i++ ) {
						string posName = PartOfSpeechTextMap[(POS)i];
						posName = posName.ToUpper()[0]+posName.Substring(1);

						fsw.Write(lineSep);
						fsw.Write($"${SynsetSortKey}{SynsetPosAbbrevs[i]}-[{wn3} {posName} Synset]");

						//fsw.Write(lineSep);
						//fsw.Write($"{WordSortKey}{SynsetPosAbbrevs[i]}-[{wn3} {posName} Word]"));
					}

					WriteSynsetNotes((n) => {
						fsw.Write(lineSep);
						fsw.Write(n.Text);
						outAndFlush(fsw);
					});
				}
			}

			using ( FileStream fs = File.Open(pathSemantics, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					fsw.Write(  $"{headerName}{wn3} Semantics");
					fsw.Write($"\n{headerSource}{source}");
					fsw.Write($"\n{headerVersion}{version}");
					fsw.Flush();

					/*foreach ( SsRelInfo ssr in SsRelList ) {
						fsw.Write(
							$"{lineSep}${SemanticSortKey}{ssr.Abbrev}-[{wn3} {ssr.Label}]"));
					}*/

					WriteSemanticNotes((n) => {
						fsw.Write(lineSep);
						fsw.Write(n.Text);
						outAndFlush(fsw);
					});

					fsw.Flush();
				}
			}

			using ( FileStream fs = File.Open(pathLexicals, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					fsw.Write(  $"{headerName}{wn3} Lexicals");
					fsw.Write($"\n{headerSource}{source}");
					fsw.Write($"\n{headerVersion}{version}");
					fsw.Flush();

					WriteLexicalNotes((n) => {
						fsw.Write(lineSep);
						fsw.Write(n.Text);
						outAndFlush(fsw);
					});

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
			Console.WriteLine("WriteSynsetNotes... (wait ~26 sec)");
			Stopwatch timer = Stopwatch.StartNew();

			foreach ( Synset syn in SynsetList ) {
				pWriteNoteFunc(syn.ToNote());
			}

			Console.WriteLine($"WriteSynsetNotes complete: {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string ToSynsetTag(this Synset pSynset) {
			var text = new StringBuilder();
			text.Append(" #");
			text.Append(SynsetHash[pSynset.Id]);
			text.Append("-wns");
			text.Append(SynsetPosAbbrevs[pSynset.PartOfSpeechId]);
			return text.ToString();
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string ToSynsetWordGroup(this Synset pSynset) {
			var text = new StringBuilder();
			List<Word> words = SynsetWordsMap[pSynset.Id];
			int wordCount = words.Count;

			text.Append(" ");

			if ( wordCount > 1 ) {
				text.Append("{ ");
			}

			for ( int i = 0 ; i < wordCount ; i++ ) {
				string name = words[i].Name;
				string tag = WordNameToTag(name, true);

				if ( i > 0 ) {
					text.Append("|");
				}

				/*if ( tag == null ) {
					text.Append(" ");
					text.Append(name);
				}
				else {*/
					text.Append("#");
					text.Append(tag);
				//}
			}

			if ( wordCount > 1 ) {
				text.Append(" }");
			}

			return text.ToString();
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string ToSynsetWordLabel(this Synset pSynset) {
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
			string glossExamples = null;
			string posShort = "";

			if ( glossSplitIndex != -1 ) {
				glossExamples = glossMain.Substring(glossSplitIndex+2);
				glossMain = glossMain.Substring(0, glossSplitIndex);
			}

			switch ( (POS)pSynset.PartOfSpeechId ) {
				case POS.Noun: posShort = "(noun)"; break;
				case POS.Verb: posShort = "(verb)"; break;
				case POS.Adjective: posShort = "(adj)"; break;
				case POS.Adverb: posShort = "(adv)"; break;
			}

			text.Append(ToSynsetWordGroup(pSynset).Substring(1));
			//text.Append(" ");
			//text.Append(posShort);

			text.Append(" >means");

			text.Append(ToSynsetTag(pSynset));
			text.Append("[");
			text.Append(posShort);
			text.Append(ToSynsetWordLabel(pSynset));
			text.Append(": \"");
			text.Append(ModifyGlossForNote(glossMain, false).Substring(1));
			text.Append("\"]");

			text.Append(" //");
			text.Append(" $");
			text.Append(SynsetSortKey);
			text.Append(pSynset.SsId);

			if ( glossExamples != null ) {
				text.Append(" ^example: ");
				text.Append(ModifyGlossForNote(glossExamples, false));
			}

			return Note.New(NoteType.SynsetMeansGloss, text.ToString());
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------* /
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

		/*--------------------------------------------------------------------------------------------* /
		private static Note ToNote(this Word pWord, string pWordTag, Synset pSynset) {
			var text = new StringBuilder();

			text.Append("#");
			text.Append(pWordTag);
			//text.Append(" %as #");
			//text.Append(PartOfSpeechTextMap[(POS)pSynset.PartOfSpeechId]);

			text.Append(" >means");
			text.Append(ToSynsetTag(pSynset));

			/*text.Append(" //");
			text.Append(" $");
			text.Append(WordSortKey);
			text.Append(SynsetPosAbbrevs[pSynset.PartOfSpeechId]);
			text.Append(pWord.Id);* /

			return Note.New(NoteType.Semantic, text.ToString());
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void WriteSemanticNotes(Action<Note> pWriteNoteFunc) {
			Console.WriteLine("WriteSemanticNotes... (wait ~3 sec)");
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
			Synset fromSyn = SynsetMap[pSemantic.Synset.Id];
			Synset toSyn = SynsetMap[pSemantic.TargetSynset.Id];
			string fromSynTag = ToSynsetTag(fromSyn);
			var text = new StringBuilder();

			text.Append(fromSynTag.Substring(1));
			//text.Append(" %as #");
			//text.Append(PartOfSpeechTextMap[(POS)fromSyn.PartOfSpeechId]);

			text.Append(" ");
			text.Append(info.Action);

			text.Append(ToSynsetTag(toSyn));

			/*if ( info.IsBiDir ) {
				text.Append(" ");
				text.Append(info.Action);
				text.Append(fromSynTag);
			}*/

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


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void WriteLexicalNotes(Action<Note> pWriteNoteFunc) {
			Console.WriteLine("WriteLexicalNotes... (wait ~3 sec)");
			Stopwatch timer = Stopwatch.StartNew();

			foreach ( Lexical lex in LexicalList ) {
				Note note = ToNote(lex);

				if ( note != null ) {
					pWriteNoteFunc(note);
				}
			}

			Console.WriteLine($"WriteLexicalNotes complete: {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		private static Note ToNote(this Lexical pLexical) {
			SynSetRelation rel = (SynSetRelation)pLexical.RelationId;

			if ( !SsRelMap.ContainsKey(rel) ) {
				return null;
			}

			SsRelInfo info = SsRelMap[rel];
			Word fromWord = WordMap[pLexical.Word.Id];
			Word toWord = WordMap[pLexical.TargetWord.Id];
			string fromWordTag = WordNameToTag(fromWord.Name, true);
			string toWordTag = WordNameToTag(toWord.Name, true);
			Synset fromSyn = SynsetMap[pLexical.Synset.Id];
			Synset toSyn = SynsetMap[pLexical.TargetSynset.Id];
			string fromSynTag = ToSynsetTag(fromSyn);
			string toSynTag = ToSynsetTag(toSyn);
			var text = new StringBuilder();

			text.Append("#");
			text.Append(fromWordTag);
			text.Append(" %of");
			text.Append(fromSynTag);

			text.Append(" ");
			text.Append(info.Action);

			text.Append(" #");
			text.Append(toWordTag);
			text.Append(" %of");
			text.Append(toSynTag);

			/*if ( info.IsBiDir ) {
				text.Append(" ");
				text.Append(info.Action);

				text.Append(" #");
				text.Append(fromWordTag);
				text.Append(" %of");
				text.Append(fromSynTag);
			}*/

			//text.Append(" // $wnlex");
			//text.Append(pLexical.Id);

			return Note.New(NoteType.Semantic, text.ToString());
		}

	}

}
