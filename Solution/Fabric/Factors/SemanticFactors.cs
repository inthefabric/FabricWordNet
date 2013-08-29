using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet.Factors {

	/*================================================================================================*/
	public class SemanticFactors {

		/*
		SELECT Word.Name, RelationId, TargWord.Name FROM Semantic 
		LEFT JOIN Word ON Word.SynsetId=Semantic.SynsetId 
		LEFT JOIN Word AS TargWord ON TargWord.SynsetId=Semantic.TargetSynsetId
		WHERE RelationId=25
		*/

		public const int MemberWordId = 157404; //[83506, 157404] //[SynsetId, WordId]
		public const int PartWordId = 12986; //[null, 12986]
		public const int SubstanceWordId = 189527; //[105211, 189527]
		public const int SimilarWordId = 2701; //[null, 2701]
		public const int RelatedWordId = 36489; //[null, 36489]
		public const int TopicWordId = 41120; //[null, 41120]
		public const int UsageWordId = 195778; //[null, 195778]
		public const int RegionWordId = 176977; //[96397, 176977]
		public const int SubsetWordId = 189508; //[105197, 189508]
		public const int CauseWordId = 86866; //[null, 86866]

		private readonly ArtifactSet vArtSet;
		private readonly SessionProvider vSessProv;
		private DateTime vStartTime;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SemanticFactors(ArtifactSet pArtSet) {
			vArtSet = pArtSet;
			vSessProv = new SessionProvider();
		}

		/*--------------------------------------------------------------------------------------------*/
		public void Start() {
			vStartTime = DateTime.UtcNow;

			using ( ISession sess = vSessProv.OpenSession() ) {
				Console.WriteLine("");
				Console.WriteLine("Starting Semantic Factors..."+TimerString());
				Console.WriteLine("");

				InsertWordToSynsetFactors(sess);

				InsertFactors(sess, WordNetEngine.SynSetRelation.Hypernym,
					DescriptorTypeId.IsA, null, true);
				InsertFactors(sess, WordNetEngine.SynSetRelation.InstanceHypernym,
					DescriptorTypeId.IsAnInstanceOf, null, true);
				InsertFactors(sess, WordNetEngine.SynSetRelation.VerbGroup,
					DescriptorTypeId.IsAnInstanceOf, SubsetWordId, true);

				InsertFactors(sess, WordNetEngine.SynSetRelation.SimilarTo,
					DescriptorTypeId.IsLike, SimilarWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.AlsoSee,
					DescriptorTypeId.IsLike, RelatedWordId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.MemberMeronym,
					DescriptorTypeId.HasA, MemberWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.PartMeronym,
					DescriptorTypeId.HasA, PartWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.SubstanceMeronym,
					DescriptorTypeId.HasA, SubstanceWordId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.TopicDomain,
					DescriptorTypeId.RefersTo, TopicWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.UsageDomain,
					DescriptorTypeId.IsAnInstanceOf, UsageWordId);
				InsertFactors(sess, WordNetEngine.SynSetRelation.RegionDomain,
					DescriptorTypeId.IsFoundIn, RegionWordId);

				InsertFactors(sess, WordNetEngine.SynSetRelation.Entailment,
					DescriptorTypeId.Requires);
				InsertFactors(sess, WordNetEngine.SynSetRelation.Cause,
					DescriptorTypeId.Produces, CauseWordId);

				InsertAttributeFactors(sess);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private string TimerString() {
			return " (SemanticFactor Time: "+(DateTime.UtcNow-vStartTime).TotalSeconds+")";
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void InsertWordToSynsetFactors(ISession pSess) {
			var artList = vArtSet.WordIdMap.Values.Where(a => a.Synset == null).ToList();
			Console.WriteLine("Found "+artList.Count+" Word Artifacts"+TimerString());

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Factors...");

				foreach ( Artifact a in artList ) {
					int targSsId = vArtSet.WordIdToSynsetIdMap[a.Word.Id];
					Artifact targArt = vArtSet.SynsetIdMap[targSsId];

					var f = new Factor();
					f.PrimaryArtifact = a;
					f.RelatedArtifact = targArt;
					f.IsDefining = true;
					f.DescriptorTypeId = (byte)DescriptorTypeId.IsA;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+a.Name+"]  "+DescriptorTypeId.IsA+"*  ["+targArt.Name+"]";
					pSess.Save(f);
				}

				Console.WriteLine("Comitting Factors..."+TimerString());
				tx.Commit();
				Console.WriteLine("Finished Factors"+TimerString());
				Console.WriteLine("");
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private void InsertFactors(ISession pSess, WordNetEngine.SynSetRelation pRel,
					DescriptorTypeId pDescTypeId, int? pDescTypeRefineWordId=null, bool pIsDef=false) {
			Console.WriteLine("Loading "+pRel+" Semantics...");

			IList<Semantic> semList = pSess.QueryOver<Semantic>()
				.Where(x => x.RelationId == (byte)pRel)
				.List();

			Console.WriteLine("Found "+semList.Count+" "+pRel+" Semantics"+TimerString());

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Factors...");

				foreach ( Semantic sem in semList ) {
					Artifact art = vArtSet.SynsetIdMap[sem.Synset.Id];
					Artifact targArt = vArtSet.SynsetIdMap[sem.TargetSynset.Id];

					var f = new Factor();
					f.Semantic = sem;
					f.PrimaryArtifact = art;
					f.RelatedArtifact = targArt;
					f.IsDefining = pIsDef;
					f.DescriptorTypeId = (byte)pDescTypeId;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+art.Name+"]  "+pDescTypeId+"  ["+targArt.Name+"] {"+pRel+"}";

					if ( pDescTypeRefineWordId != null ) {
						f.DescriptorTypeRefine = vArtSet.WordIdMap[(int)pDescTypeRefineWordId];
					}

					pSess.Save(f);
				}

				Console.WriteLine("Comitting Factors..."+TimerString());
				tx.Commit();
				Console.WriteLine("Finished Factors"+TimerString());
				Console.WriteLine("");
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void InsertAttributeFactors(ISession pSess) {
			const WordNetEngine.SynSetRelation att = WordNetEngine.SynSetRelation.Attribute;
			Console.WriteLine("Loading "+att+" Semantics...");

			IList<Semantic> semList = pSess.QueryOver<Semantic>()
				.Where(x => x.RelationId == (byte)att)
				.Fetch(x => x.Synset).Eager
				.List();

			Console.WriteLine("Found "+semList.Count+" "+att+" Semantics"+TimerString());

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Factors...");

				foreach ( Semantic sem in semList ) {
					Artifact art = vArtSet.SynsetIdMap[sem.Synset.Id];
					Artifact targArt = vArtSet.SynsetIdMap[sem.TargetSynset.Id];

					if ( sem.Synset.PartOfSpeechId != (byte)WordNetEngine.POS.Adjective &&
							sem.TargetSynset.PartOfSpeechId != (byte)WordNetEngine.POS.Noun ) {
						continue;
					}

					var f = new Factor();
					f.Semantic = sem;
					f.PrimaryArtifact = art;
					f.RelatedArtifact = targArt;
					f.IsDefining = false;
					f.DescriptorTypeId = (byte)DescriptorTypeId.RefersTo;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+art.Name+"]  "+DescriptorTypeId.RefersTo+
						"  ["+targArt.Name+"] {"+att+"}";
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