using System;
using System.IO;
using Fabric.Apps.WordNet.Data;
using LAIR.Collections.Generic;
using NHibernate;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public class MainClass {
	
		private static WordNetEngine Engine;
		
		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void Main(string[] pArgs) {
			DbBuilder.InitOnce();

			using ( ISession sess = new SessionProvider().OpenSession() ) {
				Console.WriteLine();
				Stats.PrintLexicalCountsByRel(sess);
				Console.WriteLine();
				Stats.PrintSemanticCountsByRel(sess);
				Console.WriteLine();
			}

			//BuildBaseDb();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void BuildBaseDb() {
			Console.WriteLine("Building WordNet engine...");
			string root = Directory.GetCurrentDirectory();
			Engine = new WordNetEngine(root+"/../../../../Data/WordNetDb-3.1", true);
			Console.WriteLine("WordNet engine complete.");
			Console.WriteLine("");

			DbBuilder.EraseAndRebuildDatabase();
			var sessProv = new SessionProvider();

			using ( ISession sess = sessProv.OpenSession() ) {
				sess.CreateSQLQuery("VACUUM").UniqueResult();
				sess.CreateSQLQuery("PRAGMA synchronous = OFF").UniqueResult();
				sess.CreateSQLQuery("PRAGMA journal_mode = WAL").UniqueResult();
				sess.CreateSQLQuery("PRAGMA cache_size = 60000").UniqueResult();

				using ( ITransaction tx = sess.BeginTransaction() ) {
					BuildBaseDbInserts(sess);
					tx.Commit();
				}

				sess.CreateSQLQuery("PRAGMA cache_size = 2000").UniqueResult();
				sess.CreateSQLQuery("PRAGMA journal_mode = DELETE").UniqueResult();
				sess.CreateSQLQuery("PRAGMA synchronous = NORMAL").UniqueResult();
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void BuildBaseDbInserts(ISession pSess) {
			int count = 0;
			int total = 0;
			long start = DateTime.UtcNow.Ticks;

			foreach ( WordNetEngine.POS key in Engine.AllWords.Keys ) {
				Set<string> valSet = Engine.AllWords[key];
				total += valSet.Count;
			}

			foreach ( WordNetEngine.POS key in Engine.AllWords.Keys ) {
				Set<string> valSet = Engine.AllWords[key];

				foreach ( string word in valSet ) {
					var ssg = new SynSetGroup(word, Engine.GetSynSets(word));
					ssg.InsertSynSetsAndWords(pSess);
					count++;

					if ( count % 5000  == 0 ) {
						Console.WriteLine("Syn/Word: \t"+count+" of "+total+
							" \t"+(DateTime.UtcNow.Ticks-start)/10000/1000.0+" sec");
					}

					//if ( count > 20000 ) { break; } //TEST
				}

				//if ( count > 20000 ) { break; } //TEST
			}

			count = 0;
			total = SynSetGroup.GetCachedSynsetCount();
			const int step = 5000;

			while ( true ) {
				if ( !SynSetGroup.InsertLexicalsAndSemantics(pSess, Engine, count, step) ) {
					break;
				}

				count += step;
				Console.WriteLine("Lex/Sem: \t"+count+" of "+total+
					" \t"+(DateTime.UtcNow.Ticks-start)/10000/1000.0+" sec");
			}
		}
		
	}
	
}