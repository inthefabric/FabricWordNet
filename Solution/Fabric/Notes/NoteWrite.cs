using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Fabric.Apps.WordNet.Data.Domain;
using static Fabric.Apps.WordNet.WordNetEngine;

namespace Fabric.Apps.WordNet.Notes {

	/*================================================================================================*/
	public static class NoteWrite {

		private const string Folder = @"C:\Users\Zach\Documents\Notable\WordNet\";
		private const string Wn3 = "WordNet 3.1";
		private const string LineSep = "\n\n";

		//private const string AutoIs = "@wn.verb.be_identical.>is"; //verb2622439
		private const string AutoEntail = "@wn.verb.implicate_entail.>entail"; //verb2640889
		private const string AutoPertain = "@wn.verb.have-to-do-with.>pertain"; //verb2681865
		private const string AutoRelate = "@wn.verb.colligate.>relate"; //verb715072

		private const string AutoKind = "@wn.noun.type_kind.>kind"; //noun5848697
		private const string AutoInstance = "@wn.noun.instance_example.>instance"; //noun7323507
		private const string AutoMember = "@wn.noun.member_component-part.>member"; //noun13832827
		private const string AutoPart = "@wn.noun.component-part.>part"; //noun13831419
		private const string AutoSubst = "@wn.noun.substance_component-part.>substance"; //noun19793
		private const string AutoTopic = "@wn.noun.topic_theme.>topic"; //noun6612141
		private const string AutoUsage = "@wn.noun.usage_linguistic-communication.>usage"; //noun6294112
		private const string AutoRegion = "@wn.noun.region_location.>region"; //noun8648560
		private const string AutoCause = "@wn.noun.causal-agent.>cause"; //noun7347
		private const string AutoParticiple = "@wn.noun.participle.>participle"; //noun6341521
		private const string AutoAttrib = "@wn.noun.attribute_abstract-entity.>attribute"; //noun24444
		private const string AutoOpposite = "@wn.noun.antonym.>opposite"; //noun6298695

		private const string AutoDerive = "@wn.adj.derived.>derived"; //adj701707
		private const string AutoSimilar = "@wn.adj.similar_synonymous.>similar"; //adj2390063
		private const string AutoIndirect = "@wn.adj.indirect_allusive.>indirect"; //adj770017

		private struct RelInfo {

			public SynSetRelation Rel { get; private set; }
			public string Action { get; private set; }
			public string[] Autos { get; private set; }

			public RelInfo(SynSetRelation pRel, string pAction, params string[] pAutos) {
				Rel = pRel;
				Action = pAction;
				Autos = pAutos;
			}

		}

		private static readonly RelInfo[] RelationList = {

			//directional, with ignored inverse relation
			new RelInfo(SynSetRelation.Hypernym, ">kind %of", AutoKind),
			new RelInfo(SynSetRelation.InstanceHypernym, ">instance %of", AutoInstance),
			new RelInfo(SynSetRelation.MemberHolonym, ">member %of", AutoMember),
			new RelInfo(SynSetRelation.PartHolonym, ">part %of", AutoPart),
			new RelInfo(SynSetRelation.SubstanceHolonym, ">substance %in", AutoSubst),
			new RelInfo(SynSetRelation.TopicDomain, ">topic %of", AutoTopic),
			new RelInfo(SynSetRelation.UsageDomain, ">usage %of", AutoUsage),
			new RelInfo(SynSetRelation.RegionDomain, ">region %of", AutoRegion),

			//directional, no inverse relation
			new RelInfo(SynSetRelation.VerbGroup, ">similar %to", AutoSimilar),
			new RelInfo(SynSetRelation.Entailment, ">entail}s", AutoEntail),
			new RelInfo(SynSetRelation.Cause, ">cause %of", AutoCause),
			new RelInfo(SynSetRelation.Pertainym, ">pertain}s %to", AutoPertain),
			new RelInfo(SynSetRelation.DerivedFromAdjective, ">derived %from", AutoDerive),
			new RelInfo(SynSetRelation.ParticipleOfVerb, ">participle %of", AutoParticiple),

			//bi-directional
			new RelInfo(SynSetRelation.SimilarTo, ">similar %to", AutoSimilar),
			new RelInfo(SynSetRelation.AlsoSee, ">relate}s +indirect}ly %to", AutoRelate, AutoIndirect),
			new RelInfo(SynSetRelation.Attribute, ">attribute %of", AutoAttrib),
			new RelInfo(SynSetRelation.Antonym, ">opposite %of", AutoOpposite),
			new RelInfo(SynSetRelation.DerivationallyRelated, ">relate}s %to", AutoRelate)

		};

		private static readonly Dictionary<SynSetRelation, RelInfo> RelationActionMap = 
			RelationList.ToDictionary(r => r.Rel);


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void WriteAll() {
			Console.WriteLine("\nNoteWrite.WriteAll...\n");
			Stopwatch timer = Stopwatch.StartNew();

			Console.WriteLine("WriteSynsets...");
			Write("wordnet-synsets.txt", WriteSynsets);

			Console.WriteLine("WriteWords...");
			Write("wordnet-words.txt", WriteWords);

			Console.WriteLine("WriteSemantics...");
			Write("wordnet-semantics.txt", WriteSemantics);

			Console.WriteLine("WriteLexicals...");
			Write("wordnet-lexicals.txt", WriteLexicals);

			Console.WriteLine($"\nNoteWrite.WriteAll complete: {timer.Elapsed.TotalSeconds} sec");
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void Write(string pFileName, Action<StreamWriter> pWriteFunc) {
			using ( FileStream fs = File.Open(Folder+pFileName, FileMode.Create) ) {
				using ( StreamWriter fsw = new StreamWriter(fs) ) {
					pWriteFunc(fsw);
				}
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static string GetStandardHeader(string pType) {
			return
				  "//// name: "+Wn3+" "+pType+
				"\n//// source: https://wordnet.princeton.edu"+ //"/download/current-version"+
				"\n//// version: 1";
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string GetAutoHeader(params string[] pAutos) {
			return string.Join("", pAutos.Select(a => "\n//// auto: "+a));
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static void WriteSynsets(StreamWriter pFile) {
			var sortMap = new HashSet<string>();

			pFile.Write(GetStandardHeader("Synsets"));

			pFile.Write(LineSep);
			pFile.Write($"$wn_{SynsetRelation.SynsetPosNames[1]}-[{Wn3} Noun Synset]");

			pFile.Write(LineSep);
			pFile.Write($"$wn_{SynsetRelation.SynsetPosNames[2]}-[{Wn3} Verb Synset]");

			pFile.Write(LineSep);
			pFile.Write($"$wn_{SynsetRelation.SynsetPosNames[3]}-[{Wn3} Adjective Synset]");

			pFile.Write(LineSep);
			pFile.Write($"$wn_{SynsetRelation.SynsetPosNames[4]}-[{Wn3} Adverb Synset]");

			foreach ( Synset synset in NotePrep.SynsetList ) {
				string words = string.Join(" / ", synset.WordList.Select(w => w.Name));
				SynsetRelation.SplitGloss(synset.Gloss, out string def, out string examples);
				string pos = SynsetRelation.SynsetPosNames[synset.PartOfSpeechId];
				string sortVal = synset.SsId.Substring(synset.SsId.IndexOf(':')+1);

				pFile.Write(LineSep);
				pFile.Write('@');
				pFile.Write(synset.UniqueName);

				pFile.Write('[');
				pFile.Write(words);
				pFile.Write(": (");
				pFile.Write(pos);
				pFile.Write(") \"");
				pFile.Write(def);
				pFile.Write("\"]");

				pFile.Write(" // ");
				pFile.Write("$wn_");
				pFile.Write(pos);
				pFile.Write(sortVal);

				if ( examples != null ) {
					pFile.Write(" ^example: ");
					pFile.Write(examples);
				}

				if ( !sortMap.Add(pos+sortVal) ) {
					Console.WriteLine("DUPLICATE SORT: "+pos+sortVal);
				}
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void WriteWords(StreamWriter pFile) {
			pFile.Write(GetStandardHeader("Words"));
			/*pFile.Write(GetAutoHeader(
				"@wn.adv.sometimes.+sometimes",
				"@wn.verb.signify_intend_stand-for.>mean"
			));*/

			foreach ( Synset synset in NotePrep.SynsetList ) {
				foreach ( Word word in synset.WordList ) {
					/*if ( synset.LexicalList.Any(l => l.Word == word) ||
							synset.LexicalTargetList.Any(l => l.Word == word)) {
						continue; //ignore anchor-item pairings already used by lexicals
					}*/

					pFile.Write(LineSep);
					pFile.Write('@');
					pFile.Write(synset.UniqueName);
					pFile.Write(". #");
					pFile.Write(word.Name);
					//pFile.Write(" sometimes+ >mean}s @");
					//pFile.Write(synset.UniqueName);
				}
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void WriteSemantics(StreamWriter pFile) {
			var relInfos = new List<RelInfo>();
			relInfos.Add(RelationActionMap[SynSetRelation.Hypernym]);
			relInfos.Add(RelationActionMap[SynSetRelation.InstanceHypernym]);
			relInfos.Add(RelationActionMap[SynSetRelation.MemberHolonym]);
			relInfos.Add(RelationActionMap[SynSetRelation.PartHolonym]);
			relInfos.Add(RelationActionMap[SynSetRelation.SubstanceHolonym]);
			relInfos.Add(RelationActionMap[SynSetRelation.TopicDomain]);
			relInfos.Add(RelationActionMap[SynSetRelation.UsageDomain]);
			relInfos.Add(RelationActionMap[SynSetRelation.RegionDomain]);
			relInfos.Add(RelationActionMap[SynSetRelation.VerbGroup]);
			relInfos.Add(RelationActionMap[SynSetRelation.Entailment]);
			relInfos.Add(RelationActionMap[SynSetRelation.Cause]);
			relInfos.Add(RelationActionMap[SynSetRelation.SimilarTo]);
			relInfos.Add(RelationActionMap[SynSetRelation.AlsoSee]);
			relInfos.Add(RelationActionMap[SynSetRelation.Attribute]);

			pFile.Write(GetStandardHeader("Semantics"));

			pFile.Write(GetAutoHeader(
				relInfos.SelectMany(r => r.Autos).Distinct().ToArray()
			));

			foreach ( Semantic semantic in NotePrep.SemanticList ) {
				SynSetRelation rel = (SynSetRelation)semantic.RelationId;
				string action;

				switch ( rel ) {
					case SynSetRelation.Hypernym:
					case SynSetRelation.InstanceHypernym:
					case SynSetRelation.MemberHolonym:
					case SynSetRelation.PartHolonym:
					case SynSetRelation.SubstanceHolonym:
					case SynSetRelation.TopicDomain:
					case SynSetRelation.UsageDomain:
					case SynSetRelation.RegionDomain:
					case SynSetRelation.VerbGroup:
					case SynSetRelation.Entailment:
					case SynSetRelation.Cause:
					case SynSetRelation.SimilarTo:
					case SynSetRelation.AlsoSee:
					case SynSetRelation.Attribute:
						action = RelationActionMap[rel].Action;
						break;

					case SynSetRelation.Hyponym:
					case SynSetRelation.InstanceHyponym:
					case SynSetRelation.MemberMeronym:
					case SynSetRelation.PartMeronym:
					case SynSetRelation.SubstanceMeronym:
					case SynSetRelation.TopicDomainMember:
					case SynSetRelation.UsageDomainMember:
					case SynSetRelation.RegionDomainMember:
						continue;

					default:
						Console.WriteLine("Unhandled semantic relation: "+rel+" // "+
							semantic.Synset.UniqueName+" => "+semantic.TargetSynset.UniqueName);
						continue;
				}

				pFile.Write(LineSep);
				pFile.Write('@');
				pFile.Write(semantic.Synset.UniqueName);
				pFile.Write(" ");
				pFile.Write(action);
				pFile.Write(" @");
				pFile.Write(semantic.TargetSynset.UniqueName);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private static void WriteLexicals(StreamWriter pFile) {
			var relInfos = new List<RelInfo>();
			relInfos.Add(RelationActionMap[SynSetRelation.TopicDomain]);
			relInfos.Add(RelationActionMap[SynSetRelation.UsageDomain]);
			relInfos.Add(RelationActionMap[SynSetRelation.RegionDomain]);
			relInfos.Add(RelationActionMap[SynSetRelation.VerbGroup]);
			relInfos.Add(RelationActionMap[SynSetRelation.Pertainym]);
			relInfos.Add(RelationActionMap[SynSetRelation.DerivedFromAdjective]);
			relInfos.Add(RelationActionMap[SynSetRelation.ParticipleOfVerb]);
			relInfos.Add(RelationActionMap[SynSetRelation.AlsoSee]);
			relInfos.Add(RelationActionMap[SynSetRelation.Antonym]);
			relInfos.Add(RelationActionMap[SynSetRelation.DerivationallyRelated]);

			pFile.Write(GetStandardHeader("Lexicals"));

			pFile.Write(GetAutoHeader(
				relInfos.SelectMany(r => r.Autos).Distinct().ToArray()
			));

			foreach ( Lexical lexical in NotePrep.LexicalList ) {
				SynSetRelation rel = (SynSetRelation)lexical.RelationId;
				string action;

				switch ( rel ) {
					case SynSetRelation.TopicDomain:
					case SynSetRelation.UsageDomain:
					case SynSetRelation.RegionDomain:
					case SynSetRelation.VerbGroup:
					case SynSetRelation.Pertainym:
					case SynSetRelation.DerivedFromAdjective:
					case SynSetRelation.ParticipleOfVerb:
					case SynSetRelation.AlsoSee:
					case SynSetRelation.Antonym:
					case SynSetRelation.DerivationallyRelated:
						action = RelationActionMap[rel].Action;
						break;

					case SynSetRelation.None: //handle the 3 unspecified "mellowness" relations
						action = RelationActionMap[SynSetRelation.DerivationallyRelated].Action;
						break;

					case SynSetRelation.TopicDomainMember:
					case SynSetRelation.UsageDomainMember:
					case SynSetRelation.RegionDomainMember:
						continue;

					default:
						Console.WriteLine("Unhandled lexical relation: "+rel+" // "+
							lexical.Synset.UniqueName+" => "+lexical.TargetSynset.UniqueName);
						continue;
				}

				pFile.Write(LineSep);
				pFile.Write('@');
				pFile.Write(lexical.Synset.UniqueName);
				pFile.Write(". #");
				pFile.Write(lexical.Word.Name);
				pFile.Write(" ");
				pFile.Write(action);
				pFile.Write(" @");
				pFile.Write(lexical.TargetSynset.UniqueName);
				pFile.Write(". #");
				pFile.Write(lexical.TargetWord.Name);
			}
		}

	}

}
