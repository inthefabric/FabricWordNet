using System;
using System.IO;
using LAIR.Collections.Generic;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public class MainClass {
	
		private static WordNetEngine Engine;
		
		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void Main(string[] pArgs) {
			Console.WriteLine("Building WordNet engine...");
			string root = Directory.GetCurrentDirectory();
			Engine = new WordNetEngine(root+"/../../../../Data/WordNetDb-3.1", true);
			Console.WriteLine("WordNet engine complete.");
			Console.WriteLine("");
			
			////
			
			Console.WriteLine("AllWords count: "+Engine.AllWords.Count);
			
			foreach ( WordNetEngine.POS key in Engine.AllWords.Keys ) {
				Set<string> valSet = Engine.AllWords[key];
				Console.WriteLine(" - "+key+": "+valSet.Count);
			}

			Console.WriteLine("");

			////

			const string word = "water";
			var group = new SynSetGroup(word, Engine.GetSynSets(word));
			group.OutputAll();
		}
		
	}
	
}