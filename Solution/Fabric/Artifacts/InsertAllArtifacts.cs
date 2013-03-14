using System;
using System.Collections.Generic;
using System.Linq;
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
			var test = new Dictionary<string, Artifact>();

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

				if ( test.ContainsKey("s"+ss.Id) ) {
					Console.WriteLine("S: "+ss.Id+": "+test["s"+ss.Id].Name);
					continue;
				}

				test.Add("s"+ss.Id, art);

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

					if ( test.ContainsKey("w"+w.Id) ) {
						Console.WriteLine("W: "+w.Id+": "+test["w"+w.Id].Name);
						continue;
					}

					test.Add("w"+w.Id, art);
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

				foreach ( string key in dupMap.Keys ) {
					List<ArtNode> list = dupMap[key];

					if ( list.Count > 1 ) {
						dupSets.Add(list);
					}
				}

				int nulls = 0;
				int commas = 0;
				int semis = 0;
				int parens = 0;
				int bracks = 0;
				int depths = 0;
				int ellips = 0;

				foreach ( ArtNode an in vList ) {
					string d = an.Art.Disamb;
					if ( d == null ) { ++nulls; continue; }
					if ( d.IndexOf(',') != -1 ) { ++commas; }
					if ( d.IndexOf(';') != -1 ) { ++semis; }
					if ( d.IndexOf('(') != -1 ) { ++parens; }
					if ( d.IndexOf('[') != -1 ) { ++bracks; }
					if ( d.IndexOf('>') != -1 ) { ++depths; }
					if ( d.IndexOf("...") != -1 ) { ++ellips; }
				}

				Console.WriteLine("Duplicate count before level "+level+": "+dupSets.Count+",\t stats: "+
					"n="+nulls+", c="+commas+", s="+semis+", p="+parens+
					", b="+bracks+", d="+depths+", e="+ellips);

				if ( dupSets.Count == 0 ) {
					break;
				}

				const int relLevels = 8;

				foreach ( List<ArtNode> dups in dupSets ) {
					foreach ( ArtNode a in dups ) {
						string pos = " ["+Stats.PartsOfSpeech[a.Node.SynSet.PartOfSpeechId]+"]";

						if ( level <= 4 ) {
							if ( a.IsWord ) {
								List<string> wordStrs = ArtNode.GetWordList(a.Node.SynSet);
								var dList = new List<string>();
								var max = (level-1)/2+2; //2,2,3,3

								for ( int i = 0 ; i < wordStrs.Count && dList.Count < max ; ++i ) {
									if ( wordStrs[i] != a.Art.Name ) {
										dList.Add(wordStrs[i]);
									}
								}

								a.Art.Disamb = level+") "+string.Join(", ", dList)+
									(level%2 == 0 ? pos : "");
							}
							else {
								List<SemanticNode> hypers = 
									a.Node.Relations[WordNetEngine.SynSetRelation.Hypernym];
								hypers.AddRange(
									a.Node.Relations[WordNetEngine.SynSetRelation.InstanceHypernym]);
								var max = Math.Min(hypers.Count, (level-1)/2+1); //1,1,2,2

								a.Art.Disamb = string.Join(", ", 
									hypers.GetRange(0, max).Select(x => x.SynSet.Id).ToList())+
									(level%2 == 0 ? pos : "");
							}
						}
						else if ( level <= relLevels ) {
							List<SemanticNode> nonA = dups.Where(x => x != a)
								.Select(x => x.Node).ToList();
							IDictionary<string, SemanticNode> uniMap = SemanticNode.GetRelationUnion(nonA);
							a.RelationDiff = a.Node.GetRelationDiff(uniMap);

							int count = Math.Min(a.RelationDiff.Keys.Count, (level-3)/2+1); //2,2,3,3
							List<string> keys = a.RelationDiff.Keys.ToList().GetRange(0, count);
							a.Art.Disamb = level+") "+string.Join("; ", keys)+
								(level%2 == 0 ? pos : "");
						}
						else if ( level == relLevels+1 || level == relLevels+2 ) {
							a.Art.Disamb = level+") "+ArtNode.GlossToDisamb(a.Node.SynSet.Gloss, true)+
								(level == relLevels+2 ? pos : "");
						}
						else if ( level == relLevels+3 || level == relLevels+4 ) {
							a.Art.Disamb = level+") "+ArtNode.GlossToDisamb(a.Node.SynSet.Gloss, false)+
								(level == relLevels+4 ? pos : "");
						}
						else if ( level == relLevels+5 || level == relLevels+6 ) {
							a.Art.Disamb = level+") "+a.Node.SynSet.Gloss+
								(level == relLevels+6 ? pos : "");
						}

						//an.SetDisambLevel(level);
						//an.Art.Disamb = ArtNode.TruncateString(an.Art.Disamb, 128);
					}
				}

				if ( ++level > 20 ) {
					break;
				}
			}

			Console.WriteLine("Duplicate Artifact resolution complete");
		}

	}

}