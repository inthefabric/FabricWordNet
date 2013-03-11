using System;
using System.Collections.Generic;
using System.Linq;
using LAIR.Collections.Generic;

namespace Fabric.Apps.WordNet {

	/*================================================================================================*/
	public class SynSetGroup {
	
		private readonly string vRootWord;
		private readonly Set<SynSet> vGroup;
		private readonly List<SynSet> vList;
		
		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SynSetGroup( string pRootWord, Set<SynSet> pGroup) {
			vRootWord = pRootWord;
			vGroup = pGroup;
			vList = vGroup.ToList();
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public List<SynSet> GetList() {
			return vList;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void OutputAll() {
			for ( int i = 0 ; i < vList.Count ; ++i ) {
				OutputAt(i);
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public void OutputAt(int pIndex) {
			SynSet ss = vList[pIndex];

			Console.WriteLine("-------------------------------------------");
			Console.WriteLine();
			Console.WriteLine("SynSet: "+ss.ID);
			Console.WriteLine("POS:    "+ss.POS);
			Console.Write("Words:  ");

			foreach ( string word in ss.Words ) {
				if ( word == vRootWord ) {
					Console.Write("["+word+"], ");
					continue;
				}

				Console.Write(word+", ");
			}

			Console.WriteLine();
			Console.WriteLine("Gloss:  "+ss.Gloss);
			Console.WriteLine();
			
			OutputLexical(ss.GetLexicallyRelatedWords());

			Console.WriteLine("Semantic Relations:");
			Console.WriteLine();

			foreach ( WordNetEngine.SynSetRelation rel in ss.SemanticRelations ) {
				OutputSemantic(ss, rel);
			}
			
			Console.WriteLine();
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private void OutputLexical(
					Dictionary<WordNetEngine.SynSetRelation, Dictionary<string, Set<string>>> pDict) {
			if ( pDict.Keys.Count == 0 ) {
				return;
			}

			Console.WriteLine("Lexical Relations:");
			Console.WriteLine();

			foreach ( WordNetEngine.SynSetRelation key in pDict.Keys ) {
				var setDict = pDict[key];
				
				foreach ( string setDictKey in setDict.Keys ) {
					var strSet = setDict[setDictKey];
					
					foreach ( string s in strSet ) {
						Console.Write(" - "+key+" > ");
						Console.Write(setDictKey == vRootWord ? "["+setDictKey+"]" : setDictKey);
						Console.Write(setDictKey == s ? "" : " > "+s);
						Console.WriteLine();
					}
				}
			}

			Console.WriteLine();
		}

		/*--------------------------------------------------------------------------------------------*/
		private void OutputSemantic(SynSet pSynSet, WordNetEngine.SynSetRelation pRel) {
			Set<SynSet> relSet = pSynSet.GetRelatedSynSets(pRel, false);
			Console.WriteLine(" - "+pRel);
			Console.WriteLine();

			foreach ( SynSet rs in relSet ) {
				Console.WriteLine("   - "+String.Join(", ", rs.Words));
			}

			Console.WriteLine();
		}
		
	}
	
}