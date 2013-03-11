using System;
using System.IO;
using LAIR.Collections.Generic;
using System.Collections.Generic;

namespace Fabric.Apps.FabricWordNet {

	/*================================================================================================*/
	public class MainClass {
	
		private WordNetEngine vEngine;
		//private WordNetSimilarityModel vSimModel;
		
		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void Main(string[] args) {
			var m = new MainClass();
			m.BuildEngine();
		}
		

		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void BuildEngine() {
			Console.WriteLine("BuildEngine() start");
			string root = Directory.GetCurrentDirectory();
			vEngine = new WordNetEngine(root+"/../../../../Data/WordNetDb-3.1", true);
			Console.WriteLine("BuildEngine() end");
			
			////
			
			Console.WriteLine("AllWords count: "+vEngine.AllWords.Count);
			
			foreach ( WordNetEngine.POS key in vEngine.AllWords.Keys ) {
				Set<string> valSet = vEngine.AllWords[key];
				Console.WriteLine(" - "+key+": "+valSet.Count);
				
				/*foreach ( string val in valSet ) {
					Console.WriteLine("    - "+val);
				}*/
			}
			
			Set<SynSet> synsets = vEngine.GetSynSets("water", new [] { WordNetEngine.POS.Noun });
			
			foreach ( SynSet ss in synsets ) {
				Console.WriteLine("\n"+ss.Gloss+"\n-------------");
				
				Console.WriteLine("\n * Lexically Related:");
				OutputSynSet(ss.GetLexicallyRelatedWords());
				
				Console.WriteLine("\n * Words:");
				foreach ( string word in ss.Words ) {
					Console.WriteLine("   - "+word);
				}
			}
			
			////
			
			//vSimModel = new WordNetSimilarityModel(vEngine);
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public void OutputSynSet(
					Dictionary<WordNetEngine.SynSetRelation, Dictionary<string, Set<string>>> pDict) {
			foreach ( WordNetEngine.SynSetRelation key in pDict.Keys ) {
				var setDict = pDict[key];
				
				foreach ( string setDictKey in setDict.Keys ) {
					var strSet = setDict[setDictKey];
					
					foreach ( string s in strSet ) {
						Console.WriteLine("   - "+key+" > "+setDictKey+
							(setDictKey == s ? "" : " > "+s));
					}
				}
			}
		}
		
	}
	
}