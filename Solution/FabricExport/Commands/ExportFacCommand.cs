using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Clients.Cs;
using Fabric.Clients.Cs.Api;
using NHibernate;
using NHibernate.SqlCommand;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public class ExportFacCommand : ExportBase<Factor, FabBatchNewFactor> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ExportFacCommand(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo,pMatches) {
			////
		}

		/*--------------------------------------------------------------------------------------------*/
		protected override IList<Factor> GetWordNetItemList(ISession pSess, int pItemCount) {
			Factor facAlias = null;

			return pSess.QueryOver(() => facAlias)
				.JoinQueryOver<Data.Domain.Export>(x => x.ExportList, JoinType.LeftOuterJoin)
				.Where(x => x.Factor == null)
				.Fetch(x => x.ExportList).Eager
				.OrderBy(() => facAlias.Id).Asc
				.Take(pItemCount)
				.List();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		protected override FabBatchNewFactor[] GetNewBatchList(IList<Factor> pBatch, long pIndex) {
			var factors = new FabBatchNewFactor[pBatch.Count];

			for ( int i = 0 ; i < pBatch.Count ; ++i ) {
				Factor f = pBatch[i];

				if ( vDebug ) {
					ThreadPrint(pIndex, " - Export Factor ["+f.Id+": "+
						f.PrimaryClass.Id+" -- "+f.DescriptorTypeId+" --> "+f.RelatedClass.Id+"]");
				}

				var b = new FabBatchNewFactor();
				b.BatchId = f.Id;

				b.PrimaryArtifactId = GetFabArtId(f.PrimaryClass);
				b.RelatedArtifactId = GetFabArtId(f.RelatedClass);
				b.FactorAssertionId = f.AssertionId;
				b.IsDefining = f.IsDefining;
				b.Note = f.Note;

				b.Descriptor = new FabBatchNewFactorDescriptor {
					TypeId = f.DescriptorTypeId,
					TypeRefineId = GetFabArtId(f.DescriptorTypeRefine),
					PrimaryArtifactRefineId = GetFabArtId(f.PrimaryClassRefine),
					RelatedArtifactRefineId = GetFabArtId(f.RelatedClassRefine)
				};

				b.Identor = new FabBatchNewFactorIdentor {
					TypeId = f.IdentorTypeId,
					Value = f.IdentorValue
				};

				factors[i] = b;
			}

			return factors;
		}

		/*--------------------------------------------------------------------------------------------*/
		protected override FabResponse<FabBatchResult> AddToFabric(FabricClient pFabClient,
																	FabBatchNewFactor[] pBatchNew) {
			return pFabClient.Services.Modify.AddFactors.Post(pBatchNew);
		}

		/*--------------------------------------------------------------------------------------------*/
		protected override void SetItemTypeId(ISession pSess, Data.Domain.Export pExport, int pBatchId){
			pExport.Factor = pSess.Load<Factor>(pBatchId);
			pExport.Artifact = null;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private static long GetFabArtId(Artifact pWordNetArtifact) {
			return pWordNetArtifact.ExportList[0].FabricId;
		}

	}
	
}