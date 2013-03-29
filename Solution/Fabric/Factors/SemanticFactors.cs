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


		private readonly ArtifactSet vArtSet;
		private readonly SessionProvider vSessProv;
		private readonly Dictionary<WordNetEngine.SynSetRelation, DescriptorTypeId> vDescMap;
		private DateTime vStartTime;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public SemanticFactors(ArtifactSet pArtSet) {
			vArtSet = pArtSet;
			vSessProv = new SessionProvider();

			vDescMap = new Dictionary<WordNetEngine.SynSetRelation, DescriptorTypeId>();
			vDescMap.Add(WordNetEngine.SynSetRelation.Hypernym, DescriptorTypeId.IsA);
		}

		/*--------------------------------------------------------------------------------------------*/
		public void Start() {
			vStartTime = DateTime.UtcNow;

			using ( ISession sess = vSessProv.OpenSession() ) {
				PrintTimer();
				InsertWordToSynsetFactors(sess);
				InsertFactors(sess, WordNetEngine.SynSetRelation.Hypernym, DescriptorTypeId.IsA);
				InsertFactors(sess, WordNetEngine.SynSetRelation.InstanceHypernym,
					DescriptorTypeId.IsAnInstanceOf);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void PrintTimer() {
			Console.WriteLine(" *** SemanticFactor Time: "+(DateTime.UtcNow-vStartTime).TotalSeconds);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void InsertWordToSynsetFactors(ISession pSess) {
			var artList = vArtSet.WordIdMap.Values.Where(a => a.Synset == null).ToList();
			Console.WriteLine("Found "+artList.Count+" Word Artifacts");
			PrintTimer();

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Factors...");

				foreach ( Artifact a in artList ) {
					int targSsId = vArtSet.WordIdToSynsetIdMap[a.Word.Id];
					Artifact targArt = vArtSet.SynsetIdMap[targSsId];

					var f = new Factor();
					f.PrimaryClass = a;
					f.RelatedClass = targArt;
					f.IsDefining = true;
					f.DescriptorTypeId = (byte)DescriptorTypeId.IsA;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+a.Name+"]  "+DescriptorTypeId.IsA+"*  ["+targArt.Name+"]";
					pSess.Save(f);
				}

				Console.WriteLine("Comitting Factors...");
				PrintTimer();
				tx.Commit();
				Console.WriteLine("Finished Factors");
				PrintTimer();
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private void InsertFactors(ISession pSess, WordNetEngine.SynSetRelation pRel,
																		DescriptorTypeId pDescTypeId) {
			Console.WriteLine();
			Console.WriteLine("Loading "+pRel+" Semantics...");

			IList<Semantic> semList = pSess.QueryOver<Semantic>()
				.Where(x => x.RelationId == (byte)pRel)
				.List();

			Console.WriteLine("Found "+semList.Count+" "+pRel+" Semantics");
			PrintTimer();

			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Factors...");

				foreach ( Semantic sem in semList ) {
					Artifact art = vArtSet.SynsetIdMap[sem.Synset.Id];
					Artifact targArt = vArtSet.SynsetIdMap[sem.TargetSynset.Id];

					var f = new Factor();
					f.Semantic = sem;
					f.PrimaryClass = art;
					f.RelatedClass = targArt;
					f.IsDefining = true;
					f.DescriptorTypeId = (byte)pDescTypeId;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+art.Name+"]  "+pDescTypeId+"  ["+targArt.Name+"]";
					pSess.Save(f);
				}

				Console.WriteLine("Comitting Factors...");
				PrintTimer();
				tx.Commit();
				Console.WriteLine("Finished Factors");
				PrintTimer();
			}
		}

	}

}