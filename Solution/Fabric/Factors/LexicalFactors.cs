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

		public const int AntonymArtifactId = 35857; //[null, 32354]
		public const int DerivedArtifactId = 181665; //[7292, 15701]
		public const int PertainArtifactId = 226353; //[null, 96597]
		public const int ParticipleArtifactId = 123130; //[null, 33141]

		private readonly ArtifactSet vArtSet;
		private readonly SessionProvider vSessProv;
		private DateTime vStartTime;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public LexicalFactors(ArtifactSet pArtSet) {
			vArtSet = pArtSet;
			vSessProv = new SessionProvider();
		}

		/*--------------------------------------------------------------------------------------------*/
		public void Start() {
			vStartTime = DateTime.UtcNow;

			using ( ISession sess = vSessProv.OpenSession() ) {
				Console.WriteLine("");
				Console.WriteLine("Starting Lexical Factors..."+TimerString());
				Console.WriteLine("");

				InsertFactors(sess, WordNetEngine.SynSetRelation.VerbGroup,
					DescriptorTypeId.IsAnInstanceOf, SemanticFactors.SubsetArtifactId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.AlsoSee,
					DescriptorTypeId.IsLike, SemanticFactors.RelatedArtifactId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.TopicDomain,
					DescriptorTypeId.RefersTo, SemanticFactors.TopicArtifactId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.UsageDomain,
					DescriptorTypeId.IsAnInstanceOf, SemanticFactors.UsageArtifactId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.RegionDomain,
					DescriptorTypeId.IsFoundIn, SemanticFactors.RegionArtifactId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.Antonym,
					DescriptorTypeId.IsNotLike, AntonymArtifactId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.DerivationallyRelated,
					DescriptorTypeId.IsRelatedTo, DerivedArtifactId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.ParticipleOfVerb,
					DescriptorTypeId.IsRelatedTo, ParticipleArtifactId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.Pertainym,
					DescriptorTypeId.RefersTo, PertainArtifactId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.DerivedFromAdjective,
					DescriptorTypeId.RefersTo, PertainArtifactId);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private string TimerString() {
			return " (LexicalFactor Time: "+(DateTime.UtcNow-vStartTime).TotalSeconds+")";
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void InsertFactors(ISession pSess, WordNetEngine.SynSetRelation pRel,
								DescriptorTypeId pDescTypeId, int? pDescTypeRefineArtifactId=null) {
			Console.WriteLine("Loading "+pRel+" Lexicals...");

			IList<Lexical> lexList = pSess.QueryOver<Lexical>()
				.Where(x => x.RelationId == (byte)pRel)
				.List();

			Console.WriteLine("Found "+lexList.Count+" "+pRel+" Lexicals"+TimerString());

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Factors...");

				foreach ( Lexical lex in lexList ) {
					Artifact art = vArtSet.SynsetIdMap[lex.Synset.Id];
					Artifact targArt = vArtSet.SynsetIdMap[lex.TargetSynset.Id];

					var f = new Factor();
					f.Lexical = lex;
					f.PrimaryClass = art;
					f.RelatedClass = targArt;
					f.IsDefining = true;
					f.DescriptorTypeId = (byte)pDescTypeId;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+art.Name+"]  "+pDescTypeId+"  ["+targArt.Name+"] {LEX."+pRel+"}";

					if ( pDescTypeRefineArtifactId != null ) {
						f.DescriptorTypeRefine = pSess.Load<Artifact>((int)pDescTypeRefineArtifactId);
					}

					pSess.Save(f);
				}

				Console.WriteLine("Comitting Factors..."+TimerString());
				tx.Commit();
				Console.WriteLine("Finished Factors"+TimerString());
				Console.WriteLine("");
			}
		}

	}

}