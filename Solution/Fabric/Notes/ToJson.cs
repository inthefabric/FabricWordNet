using System;
using System.IO;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;

namespace Fabric.Apps.WordNet.Notes
{

	/*================================================================================================*/
	public static class ToJson {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void WriteAll() {
			Console.WriteLine("\nToJson.WriteAll...\n");

			const string path = "/Users/zachkinstner/Documents/ShipOfTheseus/Kabb/";

			using ( FileStream fs = File.Open(path+"wordnet.synset.txt", FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					foreach ( Synset syn in NotePrep.SynsetList ) {
						fsw.Write(syn.Id);
						fsw.Write('\t');
						fsw.Write(syn.SsId);
						fsw.Write('\t');
						fsw.Write(syn.PartOfSpeechId);
						fsw.Write('\t');
						fsw.Write(syn.Gloss);
						fsw.Write('\n');
					}
				}
			}

			using ( FileStream fs = File.Open(path+"wordnet.lexical.txt", FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					foreach ( Lexical lex in NotePrep.LexicalList ) {
						fsw.Write(lex.Id);
						fsw.Write('\t');
						fsw.Write(lex.RelationId);
						fsw.Write('\t');
						fsw.Write(lex.Synset.Id);
						fsw.Write('\t');
						fsw.Write(lex.Word.Id);
						fsw.Write('\t');
						fsw.Write(lex.TargetSynset.Id);
						fsw.Write('\t');
						fsw.Write(lex.TargetWord.Id);
						fsw.Write('\n');
					}
				}
			}

			using ( FileStream fs = File.Open(path+"wordnet.word.txt", FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					foreach ( Word word in NotePrep.WordList ) {
						fsw.Write(word.Id);
						fsw.Write('\t');
						fsw.Write(word.Synset.Id);
						fsw.Write('\t');
						fsw.Write(word.Name);
						fsw.Write('\n');
					}
				}
			}

			using ( FileStream fs = File.Open(path+"wordnet.semantic.txt", FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					foreach ( Semantic sem in NotePrep.SemanticList ) {
						fsw.Write(sem.Id);
						fsw.Write('\t');
						fsw.Write(sem.RelationId);
						fsw.Write('\t');
						fsw.Write(sem.Synset.Id);
						fsw.Write('\t');
						fsw.Write(sem.TargetSynset.Id);
						fsw.Write('\n');
					}
				}
			}

			////

			using ( FileStream fs = File.Open(path+"wordnet.js", FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					fsw.WriteLine("const wordnet = {");
					fsw.WriteLine("\nsynsets:{");

					foreach ( Synset syn in NotePrep.SynsetList ) {
						fsw.Write(syn.Id);
						fsw.Write(":[");
						fsw.Write(syn.SsId.Substring(syn.SsId.IndexOf(':')+1));
						fsw.Write(',');
						fsw.Write(syn.PartOfSpeechId);
						//fsw.Write(",");
						//fsw.Write(syn.Gloss);
						fsw.Write("],\n");
					}

					fsw.WriteLine("},");
					fsw.WriteLine("\nlexicals:[");

					foreach ( Lexical lex in NotePrep.LexicalList ) {
						fsw.Write("[");
						//fsw.Write(lex.Id);
						//fsw.Write(',');
						fsw.Write(lex.RelationId);
						fsw.Write(',');
						fsw.Write(lex.Synset.Id);
						fsw.Write(',');
						fsw.Write(lex.Word.Id);
						fsw.Write(',');
						fsw.Write(lex.TargetSynset.Id);
						fsw.Write(',');
						fsw.Write(lex.TargetWord.Id);
						fsw.Write("],\n");
					}

					fsw.WriteLine("],");
					fsw.WriteLine("\nwords:[");

					foreach ( Word word in NotePrep.WordList ) {
						fsw.Write("[");
						//fsw.Write(word.Id);
						//fsw.Write(',');
						fsw.Write(word.Synset.Id);
						fsw.Write(",'");
						fsw.Write(word.Name);
						fsw.Write("'],\n");
					}

					fsw.WriteLine("],");
					fsw.WriteLine("\nsemantics:[");

					foreach ( Semantic sem in NotePrep.SemanticList ) {
						fsw.Write("[");
						//fsw.Write(sem.Id);
						//fsw.Write(',');
						fsw.Write(sem.RelationId);
						fsw.Write(',');
						fsw.Write(sem.Synset.Id);
						fsw.Write(',');
						fsw.Write(sem.TargetSynset.Id);
						fsw.Write("],\n");
					}

					fsw.WriteLine("],");
					fsw.WriteLine("\n};");
				}
			}
		}

	}

}
