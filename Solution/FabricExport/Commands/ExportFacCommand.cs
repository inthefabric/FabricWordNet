using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Clients.Cs;
using Fabric.Clients.Cs.Api;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public class ExportFacCommand : ExportBase<Factor, FabBatchNewFactor> {

		private static IDictionary<int, long> ArtMap;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ExportFacCommand(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo,pMatches){}

		/*--------------------------------------------------------------------------------------------*/
		protected override IList<Factor> GetWordNetItemList(ISession pSess, int pItemCount) {
			Factor facAlias = null;
			CommIo.Print("Loading Factors segment...");

			IList<Factor> facs = pSess.QueryOver(() => facAlias)
				.JoinQueryOver<Data.Domain.Export>(x => x.ExportList, JoinType.LeftOuterJoin)
				.Where(x => x.Factor == null)
				.OrderBy(() => facAlias.Id).Asc
				.Take(pItemCount)
				.List();

			CommIo.Print("Loading all Artifact Exports...");

			if ( ArtMap == null ) {
				Artifact artAlias = null;

				IList<object[]> list = pSess.QueryOver(() => artAlias)
					.SelectList(sl => sl
						.Select(x => x.Id)
						.SelectSubQuery(
							QueryOver.Of<Data.Domain.Export>()
								.Where(x => x.Artifact.Id == artAlias.Id)
								.Select(x => x.FabricId)
						)
					)
					.List<object[]>();

				CommIo.Print("Mapping WordNet ArtifactIds to Fabric ArtifactIds...");
				ArtMap = new Dictionary<int, long>();

				foreach ( object[] a in list ) {
					ArtMap.Add((int)a[0], (long)a[1]);
				}
			}
			else {
				CommIo.Print(" * "+ArtMap.Keys.Count+" Cached!");
			}

			return facs;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		protected override FabBatchNewFactor[] GetNewBatchList(IList<Factor> pBatch, long pIndex) {
			var factors = new FabBatchNewFactor[pBatch.Count];

			for ( int i = 0 ; i < pBatch.Count ; ++i ) {
				Factor f = pBatch[i];

				if ( vDebug ) {
					ThreadPrint(pIndex, " - Export Factor ["+f.Id+": "+
						f.PrimaryArtifact.Id+" -- "+f.DescriptorTypeId+" --> "+f.RelatedArtifact.Id+"]");
				}

				var b = new FabBatchNewFactor();
				b.BatchId = f.Id;

				b.PrimaryArtifactId = GetFabArtId(f.PrimaryArtifact);
				b.RelatedArtifactId = GetFabArtId(f.RelatedArtifact);
				b.FactorAssertionId = f.AssertionId;
				b.IsDefining = f.IsDefining;
				b.Note = null; //f.Note;

				b.Descriptor = new FabBatchNewFactorDescriptor {
					TypeId = f.DescriptorTypeId,
					TypeRefineId = (f.DescriptorTypeRefine == null ? (long?)null : 
						GetFabArtId(f.DescriptorTypeRefine)),
					PrimaryArtifactRefineId = (f.PrimaryClassRefine == null ? (long?)null : 
						GetFabArtId(f.PrimaryClassRefine)),
					RelatedArtifactRefineId = (f.RelatedClassRefine == null ? (long?)null : 
						GetFabArtId(f.RelatedClassRefine))
				};

				if ( f.IdentorTypeId != 0 ) {
					b.Identor = new FabBatchNewFactorIdentor {
						TypeId = f.IdentorTypeId,
						Value = f.IdentorValue
					};
				}

				/*ThreadPrint(pIndex, "F: "+f.Id+
					" / "+b.PrimaryArtifactId+
					" / "+b.RelatedArtifactId+
					" / "+b.Descriptor.PrimaryArtifactRefineId+
					" / "+b.Descriptor.RelatedArtifactRefineId);*/

				factors[i] = b;
			}

			return factors;
		}

		/*--------------------------------------------------------------------------------------------*/
		protected override FabResponse<FabBatchResult> AddToFabric(FabricClient pFabClient,
																	FabBatchNewFactor[] pBatchNew) {
			return pFabClient.Services.Modify.AddFactors.Post(pBatchNew);

			/*var fr = new FabResponse<FabBatchResult>();
			fr.Data = new List<FabBatchResult>();

			foreach ( FabBatchNewFactor nf in pBatchNew ) {
				var br = new FabBatchResult();
				br.BatchId = nf.BatchId;
				br.ResultId = 1;
				fr.Data.Add(br);
			}

			return fr;*/
		}

		/*--------------------------------------------------------------------------------------------*/
		protected override void SetItemTypeId(ISession pSess, Data.Domain.Export pExport, int pBatchId){
			pExport.Factor = pSess.Load<Factor>(pBatchId);
			pExport.Artifact = null;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private long GetFabArtId(Artifact pWordNetArtifact) {
			return ArtMap[pWordNetArtifact.Id];
		}

	}
	
}