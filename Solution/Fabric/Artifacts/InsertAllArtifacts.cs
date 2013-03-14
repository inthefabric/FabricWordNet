using System;
using System.Collections.Generic;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet.Artifacts {

	/*================================================================================================*/
	public class InsertAllArtifacts {

		private readonly SemanticNodes vNodes;
		private readonly List<ArtNode> vList;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public InsertAllArtifacts(SemanticNodes pNodes) {
			vNodes = pNodes;
			vList = new List<ArtNode>();

			BuildArtifacts();
			ResolveArtifactDuplicates();
		}

		/*--------------------------------------------------------------------------------------------*/
		public void Insert(ISession pSess) {
			Console.WriteLine("Inserting "+vList.Count+" Artifacts...");

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				foreach ( ArtNode an in vList ) {
					pSess.Save(an.Art);
				}

				tx.Commit();
			}

			Console.WriteLine("Insert complete");
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void BuildArtifacts() {
			foreach ( SemanticNode n in vNodes.NodeList ) {
				Synset ss = n.SynSet;
				List<string> words = ArtNode.GetWordList(ss);
				string pos = Stats.PartsOfSpeech[ss.PartOfSpeechId]+"";

				Artifact art = new Artifact();
				art.Name = string.Join(", ", (words.Count > 5 ? words.GetRange(0,5) : words));
				art.Name = ArtNode.TruncateString(art.Name, 128);
				art.Note = ArtNode.TruncateString(pos+": "+ss.Gloss, 256);
				art.Synset = ss;
				art.Word = (words.Count == 1 ? ss.WordList[0] : null);
				vList.Add(new ArtNode { Art = art, Node = n, Word = art.Word });

				if ( art.Word != null ) {
					continue;
				}

				for ( int i = 0 ; i < words.Count ; ++i ) {
					Word w = ss.WordList[i];

					art = new Artifact();
					art.Name = ArtNode.TruncateString(words[i], 128);
					art.Note = ArtNode.TruncateString(pos+": "+ss.Gloss, 256);
					art.Word = w;
					vList.Add(new ArtNode { Art = art, Node = n, Word = art.Word, IsWord = true });
				}
			}

			Console.WriteLine("Created "+vList.Count+" Artifacts...");
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private Dictionary<string, List<ArtNode>> GetDuplicateMap() {
			var map = new Dictionary<string, List<ArtNode>>();

			foreach ( ArtNode an in vList ) {
				string key = an.Art.Name+"||"+an.Art.Disamb;

				if ( !map.ContainsKey(key) ) {
					map.Add(key, new List<ArtNode>());
				}

				map[key].Add(an);
			}

			return map;
		}

		/*--------------------------------------------------------------------------------------------*/
		private void ResolveArtifactDuplicates() {
			int level = 1;

			while ( true ) {
				Dictionary<string, List<ArtNode>> dupMap = GetDuplicateMap();
				var dupSets = new List<List<ArtNode>>();
				int dupCount = 0;

				foreach ( string key in dupMap.Keys ) {
					List<ArtNode> list = dupMap[key];

					if ( list.Count > 1 ) {
						dupSets.Add(list);
						dupCount += dupSets.Count;
					}
				}

				Console.WriteLine("Duplicate count before level "+level+": "+
					dupSets.Count+" / "+dupCount);

				if ( dupSets.Count == 0 ) {
					break;
				}

				foreach ( List<ArtNode> dups in dupSets ) {
					foreach ( ArtNode an in dups ) {
						switch ( level ) {
							case 1:
								an.Art.Disamb = an.GetUniqueWordString(dups);
								break;

							case 2:
								an.Art.Disamb = ArtNode.GlossToDisamb(an.Node.SynSet.Gloss, true);
								break;

							case 3:
								an.Art.Disamb = ArtNode.GlossToDisamb(an.Node.SynSet.Gloss, false);
								break;

							case 4:
								an.Art.Disamb = an.Node.SynSet.Gloss;
								break;
						}

						an.Art.Disamb = level+") "+an.Art.Disamb;
						an.Art.Disamb = ArtNode.TruncateString(an.Art.Disamb, 128);
					}
				}

				if ( ++level > 6 ) {
					break;
				}
			}

			Console.WriteLine("Duplicate Artifact resolution complete");
		}

	}

}