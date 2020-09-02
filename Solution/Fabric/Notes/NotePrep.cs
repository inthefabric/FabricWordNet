using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using NHibernate;

namespace Fabric.Apps.WordNet.Notes {

	/*================================================================================================*/
	public static class NotePrep {

		//see: https://wordnet.princeton.edu/documentation/wngloss7wn
		//see: https://globalwordnet.github.io/gwadoc/

		public static List<Synset> SynsetList { get; private set; }
		public static List<Word> WordList { get; private set; }
		public static IList<Semantic> SemanticList { get; private set; }
		public static IList<Lexical> LexicalList { get; private set; }

		public static Dictionary<string, List<Synset>> WordnameSynsetsMap { get; private set; }
		//public static List<WordnameSynsets> WordameSynsetList { get; private set; }
		public static SynsetNamingNode SynsetNamingRoot { get; private set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void Process() {
			Console.WriteLine("\nNotePrep.Process...\n");
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

			Console.WriteLine("FillReferences...");
			FillReferences();
			Console.WriteLine("SortWords...");
			SortWords();

			Console.WriteLine("BuildSynsetNamingTree...");
			BuildSynsetNamingTree();
			Console.WriteLine("SimplifySynsetUniqueParts...");
			SimplifySynsetUniqueParts(SynsetList, 0);
			Console.WriteLine("GenerateSynsetUniqueNames...");
			GenerateSynsetUniqueNames();

			//Console.WriteLine("WriteSynsetNamesToFile...");
			//WriteSynsetNamesToFile();

			Console.WriteLine($"\nNotePrep.Process complete: {timer.Elapsed.TotalSeconds} sec");
		}

		/*--------------------------------------------------------------------------------------------*/
		public static int SortWordsByWordnameCount(Word pWordA, Word pWordB) {
			return WordnameSynsetsMap[pWordA.Name].Count-WordnameSynsetsMap[pWordB.Name].Count;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void FillReferences() {
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
				word.LexicalTargetList = new List<Lexical>();

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
				lexical.TargetWord.LexicalTargetList.Add(lexical);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void SortWords() {
			WordnameSynsetsMap = new Dictionary<string, List<Synset>>();

			foreach ( Word word in WordList ) {
				string wordname = word.Name;

				if ( !WordnameSynsetsMap.ContainsKey(wordname) ) {
					WordnameSynsetsMap.Add(wordname, new List<Synset>());
				}

				WordnameSynsetsMap[wordname].Add(word.Synset);
			}

			/*WordameSynsetList = WordnameSynsetsMap
				.Select(p => new WordnameSynsets { Name = p.Key, Synsets = p.Value })
				.ToList();

			WordameSynsetList.Sort((a,b) => b.Synsets.Count-a.Synsets.Count);*/

			foreach ( Synset synset in SynsetList ) {
				((List<Word>)synset.WordList).Sort(SortWordsByWordnameCount);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void BuildSynsetNamingTree() {
			SynsetNamingRoot = new SynsetNamingNode(null, "wn", 0);
			SynsetNamingRoot.SynRels.AddRange(SynsetList.Select(s => new SynsetRelation(s)));
			SynsetNamingRoot.ConvertSynNamesIntoChildNodes();
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void SimplifySynsetUniqueParts(List<Synset> pSynsets, int pDepth) {
			var synPartMap = new Dictionary<string, List<Synset>>();
			int partI = pDepth+2;

			foreach ( Synset synset in pSynsets ) {
				if ( partI >= synset.UniqueParts.Count ) {
					continue;
				}

				string key = synset.UniqueParts[partI];

				if ( !synPartMap.ContainsKey(key) ) {
					synPartMap.Add(key, new List<Synset>());
				}

				synPartMap[key].Add(synset);
			}

			if ( synPartMap.Count == 1 ) {
				foreach ( List<Synset> singleKeySynsets in synPartMap.Values ) {
					foreach ( Synset synset in singleKeySynsets ) {
						//string before = synset.UniqueParts[partI]+"  //  "+
						//	string.Join(".", synset.UniqueParts);

						synset.UniqueParts.RemoveAt(partI);

						//Console.WriteLine("SIMPLIFY:  "+pDepth+"  //  "+
						//	before+"  =>  "+string.Join(".", synset.UniqueParts));
					}
				}

				SimplifySynsetUniqueParts(pSynsets, pDepth); //get the new values at this depth
				return;
			}

			foreach ( KeyValuePair<string, List<Synset>> pair in synPartMap ) {
				SimplifySynsetUniqueParts(pair.Value, pDepth+1);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void GenerateSynsetUniqueNames() {
			const int maxLen = 64;

			foreach ( Synset synset in SynsetList ) {
				synset.UniqueName = synset.UniqueParts[0];

				for ( var i = 1 ; i < synset.UniqueParts.Count ; i++ ) {
					synset.UniqueName += (i > 2 ? '_' : '.')+synset.UniqueParts[i];
				}

				if ( synset.UniqueName.Length > maxLen ) {
					string origName = synset.UniqueName;
					synset.UniqueName = synset.UniqueName.Substring(0, maxLen);

					int dashI = synset.UniqueName.LastIndexOf('-');
					int underI = synset.UniqueName.LastIndexOf('_');

					if ( dashI > underI ) {
						synset.UniqueName = synset.UniqueName.Substring(0, dashI);
					}

					//Console.WriteLine("SHORTEN: "+origName+"  =>  "+synset.UniqueName);
				}
			}

			SynsetList.Sort((a,b) => {
				int posDiff = a.PartOfSpeechId-b.PartOfSpeechId;
				return (posDiff == 0 ? a.UniqueName.CompareTo(b.UniqueName) : posDiff);
			});
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void WriteSynsetNamesToFile() {
			List<string> uniqueSynsetNames = SynsetList.Select(s => s.UniqueName).ToList();
			uniqueSynsetNames.Sort();

			var uniqueCheckMap = new HashSet<string>();

			const string folder = @"D:\Work\AEI\Notable\Docs\";
			const string pathTree = folder+"synset-tree.txt";
			const string pathNames = folder+"synset-names.txt";

			using ( FileStream fs = File.Open(pathTree, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					fsw.Write(SynsetNamingRoot.ToTreeString());
				}
			}

			using ( FileStream fs = File.Open(pathNames, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					foreach ( string uniqueSynsetName in uniqueSynsetNames ) {
						fsw.Write(uniqueSynsetName);

						if ( !uniqueCheckMap.Add(uniqueSynsetName) ) {
							fsw.Write(" <DUPLICATE>");
							Console.WriteLine("DUPLICATE: "+uniqueSynsetName);
						}

						fsw.Write('\n');
					}
				}
			}
		}

	}

}
