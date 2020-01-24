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
using NHibernate.Transform;
using static Fabric.Apps.WordNet.WordNetEngine;

namespace Fabric.Apps.WordNet.Notes {

	/*================================================================================================*/
	public static class NoteUtil {

		private const string SynsetSortKey = "wn_syn";
		private static readonly string[] SynsetPosAbbrevs = { "", "n", "v", "adj", "adv" };

		private static Dictionary<WordNetEngine.POS, string> PartOfSpeechTextMap;
		private static IList<Synset> AllSynsetsIncludingWords;
		private static HashSet<string> WordMap;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void BuildNotes() {
			Console.WriteLine("BuildNotes...");
			DbBuilder.UpdateSchema();

			BuildPosMap();

			using ( ISession sess = new SessionProvider().OpenSession() ) {
				BuildWordNet.SetDbStateBeforeBatchInsert(sess);
				GetAllSynsetsIncludingWords(sess);
				BuildWordMap();
				BuildSynsetNotes(sess);
				BuildWordNet.SetDbStateAfterBatchInsert(sess);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void BuildPosMap() {
			PartOfSpeechTextMap = new Dictionary<WordNetEngine.POS, string>();

			foreach ( WordNetEngine.POS pos in Enum.GetValues(typeof(WordNetEngine.POS)) ) {
				PartOfSpeechTextMap.Add(pos, pos.ToString().ToLower());
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void GetAllSynsetsIncludingWords(ISession pSess) {
			Console.WriteLine("GetAllSynsetsIncludingWords... (wait ~45 sec)");
			var timer = Stopwatch.StartNew();

			AllSynsetsIncludingWords = pSess.QueryOver<Synset>()
				.Fetch(SelectMode.Fetch, x => x.WordList)
				.TransformUsing(Transformers.DistinctRootEntity)
				//.Take(2000)
				.List();

			List<Synset> allSynsets = AllSynsetsIncludingWords.ToList();
			HashSet<int> ssidMap = new HashSet<int>();

			foreach ( Synset synset in allSynsets ) {
				string[] split = synset.SsId.Split(':');

				synset.SsId = "_"+SynsetPosAbbrevs[synset.PartOfSpeechId]+split[1];
				synset.SortValue = int.Parse(split[1])+(synset.PartOfSpeechId-1)*100000000;

				if ( ssidMap.Contains(synset.SortValue) ) {
					Console.WriteLine("DUP: "+synset.SsId+" / "+synset.SortValue);
				}

				ssidMap.Add(synset.SortValue);
			}

			allSynsets.Sort((a,b) => a.SortValue-b.SortValue);
			AllSynsetsIncludingWords = allSynsets;

			Console.WriteLine("GetAllSynsetsIncludingWords complete: "+
				$"{AllSynsetsIncludingWords.Count} results, {timer.Elapsed.Seconds} sec");
		}

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
		private static void BuildWordMap() {
			Console.WriteLine("BuildWordMap... (wait ~2 sec)");
			var timer = Stopwatch.StartNew();

			WordMap = new HashSet<string>();

			foreach ( Synset syn in AllSynsetsIncludingWords ) {
				foreach ( Word word in syn.WordList ) {
					string tag = WordNameToTag(word.Name);

					if ( tag == null ) {
						continue;
					}

					WordMap.Add(tag.ToLower());
				}
			}

			Console.WriteLine("BuildWordMap complete: "+
				$"{WordMap.Count} results, {timer.Elapsed.Seconds} sec");
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

		/*--------------------------------------------------------------------------------------------*/
		private static void BuildSynsetNotes(ISession pSess) {
			Console.WriteLine("BuildSynsetNotes... (wait ~35 sec)");
			var timer = Stopwatch.StartNew();

			const int batchSize = 100;
			int synCount = AllSynsetsIncludingWords.Count;
			int synI = 0;
			const string path = "/Users/zachkinstner/Downloads/sot/wn.json";

			using ( FileStream fs = File.Open(path, FileMode.Create) ) {
			using ( StreamWriter fsw = new StreamWriter(fs) ) {

			fsw.Write("{\"notes\":[");
			fsw.Flush();

			int noteId = 1000000;

			fsw.Write( "\n"+ToNoteJson(noteId++, $"${SynsetSortKey}_"+
				$"{SynsetPosAbbrevs[(int)POS.Noun]}-[WordNet 3.1: Noun Synsets]"));
			fsw.Write(",\n"+ToNoteJson(noteId++, $"${SynsetSortKey}_"+
				$"{SynsetPosAbbrevs[(int)POS.Verb]}-[WordNet 3.1: Verb Synsets]"));
			fsw.Write(",\n"+ToNoteJson(noteId++, $"${SynsetSortKey}_"+
				$"{SynsetPosAbbrevs[(int)POS.Adjective]}-[WordNet 3.1: Adjective Synsets]"));
			fsw.Write(",\n"+ToNoteJson(noteId++, $"${SynsetSortKey}_"+
				$"{SynsetPosAbbrevs[(int)POS.Adverb]}-[WordNet 3.1: Adverb Synsets]"));

			for ( int batchI = 0 ; synI < synCount ; batchI++ ) {
				//using ( ITransaction tx = pSess.BeginTransaction() ) {
					for ( int txI = 0 ; txI < batchSize && synI < synCount ; txI++ ) {
						Synset syn = AllSynsetsIncludingWords[synI];
						Note note = syn.ToNote(noteId);
						fsw.Write(",\n"+ToNoteJson(noteId, note.Text));
						//Console.WriteLine(batchI+"/"+synI+"   --------   "+note.Text);
						//pSess.Save(syn.ToNote());
						synI++;
						noteId++;
					}

					//tx.Commit();
					fsw.Flush();
					Console.WriteLine($"BuildSynsetNotes batch: {batchI}, "+
						$"{timer.Elapsed.Seconds} sec");
				//}
			}

			fsw.Write("\n]}");
			fsw.Flush();
			}} //file usings

			//File.WriteAllText("/Users/zachkinstner/Downloads/sot/wordnet.json", json);
			Console.WriteLine($"BuildSynsetNotes complete: {timer.Elapsed.Seconds} sec");
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

				if ( match?.Success == true && WordMap.Contains(match.Value.ToLower()) ) {
					text.Append(glossToken.Insert(match.Index, "#"));
				}
				else {
					text.Append(glossToken);
				}
			}

			return text.ToString();
		}

		/*--------------------------------------------------------------------------------------------*/
		private static Note ToNote(this Synset pSynset, int pNoteId) {
			var text = new StringBuilder();
			int wordCount = pSynset.WordList.Count;

			if ( wordCount > 1 ) {
				text.Append("{");
			}

			for ( int i = 0 ; i < wordCount ; i++ ) {
				string name = pSynset.WordList[i].Name;
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

			text.Append(" %as #");
			text.Append(PartOfSpeechTextMap[(WordNetEngine.POS)pSynset.PartOfSpeechId]);
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

	}

}
