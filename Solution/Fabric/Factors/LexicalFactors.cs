using System;
using System.Collections.Generic;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet.Factors {

	/*================================================================================================*/
	public class LexicalFactors {

		/*
		SELECT Word.Name, RelationId, TargWord.Name FROM Lexical
		LEFT JOIN Word ON Word.SynsetId=Lexical.SynsetId 
		LEFT JOIN Word AS TargWord ON TargWord.SynsetId=Lexical.TargetSynsetId
		WHERE RelationId=27
		*/

		public const int AntonymWordId = 32354; //[null, 32354]
		public const int DerivationWordId = 105936; //[55008, 105936]
		public const int PertainWordId = 96597; //[null, 96597]
		public const int ParticipleWordId = 33141; //[null, 33141]

		private readonly ArtifactSet vArtSet;
		private readonly SessionProvider vSessProv;
		private DateTime vStartTime;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public LexicalFactors(ArtifactSet pArtSet) {
			vArtSet = pArtSet;
			vSessProv = new SessionProvider();

			SemanticFactors.AssertWord(vArtSet, AntonymWordId, "antonym");
			SemanticFactors.AssertWord(vArtSet, DerivationWordId, "derivation");
			SemanticFactors.AssertWord(vArtSet, PertainWordId, "pertain");
			SemanticFactors.AssertWord(vArtSet, ParticipleWordId, "participle");
		}

		/*--------------------------------------------------------------------------------------------*/
		public void Start() {
			vStartTime = DateTime.UtcNow;

			using ( ISession sess = vSessProv.OpenSession() ) {
				Console.WriteLine("");
				Console.WriteLine("Starting Lexical Factors..."+TimerString());
				Console.WriteLine("");

				InsertFactors(sess, WordNetEngine.SynSetRelation.VerbGroup,
					DescriptorTypeId.IsAnInstanceOf, SemanticFactors.SubsetWordId, true);

				InsertFactors(sess, WordNetEngine.SynSetRelation.AlsoSee,
					DescriptorTypeId.IsLike, SemanticFactors.RelatedWordId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.TopicDomain,
					DescriptorTypeId.RefersTo, SemanticFactors.TopicWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.UsageDomain,
					DescriptorTypeId.IsAnInstanceOf, SemanticFactors.UsageWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.RegionDomain,
					DescriptorTypeId.IsFoundIn, SemanticFactors.RegionWordId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.Antonym,
					DescriptorTypeId.IsNotLike, AntonymWordId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.DerivationallyRelated,
					DescriptorTypeId.IsRelatedTo, DerivationWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.ParticipleOfVerb,
					DescriptorTypeId.IsRelatedTo, ParticipleWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.Pertainym,
					DescriptorTypeId.RefersTo, PertainWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.DerivedFromAdjective,
					DescriptorTypeId.RefersTo, PertainWordId);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private string TimerString() {
			return " (LexicalFactor Time: "+(DateTime.UtcNow-vStartTime).TotalSeconds+")";
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void InsertFactors(ISession pSess, WordNetEngine.SynSetRelation pRel,
					DescriptorTypeId pDescTypeId, int? pDescTypeRefineWordId=null, bool pIsDef=false) {
			Console.WriteLine("Loading "+pRel+" Lexicals...");

			IList<Lexical> lexList = pSess.QueryOver<Lexical>()
				.Where(x => x.RelationId == (byte)pRel)
				.List();

			Console.WriteLine("Found "+lexList.Count+" "+pRel+" Lexicals"+TimerString());

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Factors...");
				var oppMap = new HashSet<string>();
				int oppSkips = 0;

				foreach ( Lexical lex in lexList ) {
					Artifact art = (vArtSet.WordIdMap.ContainsKey(lex.Word.Id) ? 
						vArtSet.WordIdMap[lex.Word.Id] : vArtSet.SynsetIdMap[lex.Synset.Id]);
					Artifact targArt = (vArtSet.WordIdMap.ContainsKey(lex.TargetWord.Id) ? 
						vArtSet.WordIdMap[lex.TargetWord.Id] :vArtSet.SynsetIdMap[lex.TargetSynset.Id]);

					if ( oppMap.Contains(targArt.Id+"|"+art.Id) ) {
						oppSkips++;
						continue; //avoid creating a B->A "duplicate" of an existing A->B Factor
					}

					oppMap.Add(art.Id+"|"+targArt.Id);

					var f = new Factor();
					f.Lexical = lex;
					f.PrimaryArtifact = art;
					f.RelatedArtifact = targArt;
					f.IsDefining = pIsDef;
					f.DescriptorTypeId = (byte)pDescTypeId;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+art.Name+"]  "+pDescTypeId+"  ["+targArt.Name+"] {LEX."+pRel+"}";

					if ( pDescTypeRefineWordId != null ) {
						f.DescriptorTypeRefine = vArtSet.WordIdMap[(int)pDescTypeRefineWordId];
					}

					pSess.Save(f);
				}

				Console.WriteLine("Skipped "+oppSkips+" reversed Factors..."+TimerString());
				Console.WriteLine("Comitting Factors..."+TimerString());
				tx.Commit();
				Console.WriteLine("Finished Factors"+TimerString());
				Console.WriteLine("");
			}
		}

	}

}