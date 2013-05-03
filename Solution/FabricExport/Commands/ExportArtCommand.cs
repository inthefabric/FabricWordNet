using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Clients.Cs;
using Fabric.Clients.Cs.Api;
using NHibernate;
using NHibernate.SqlCommand;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public class ExportArtCommand : ExportBase<Artifact, FabBatchNewClass> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ExportArtCommand(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo,pMatches) {
			////
		}

		/*--------------------------------------------------------------------------------------------*/
		protected override IList<Artifact> GetWordNetItemList(ISession pSess, int pItemCount) {
			Artifact artAlias = null;

			return pSess.QueryOver(() => artAlias)
				.JoinQueryOver<Data.Domain.Export>(x => x.ExportList, JoinType.LeftOuterJoin)
				.Where(x => x.Artifact == null)
				.Fetch(x => x.ExportList).Eager
				.OrderBy(() => artAlias.Id).Asc
				.Take(pItemCount)
				.List();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		protected override FabBatchNewClass[] GetNewBatchList(IList<Artifact> pBatch, long pIndex) {
			var classes = new FabBatchNewClass[pBatch.Count];

			for ( int i = 0 ; i < pBatch.Count ; ++i ) {
				Artifact a = pBatch[i];

				if ( vDebug ) ThreadPrint(pIndex, " - Export Artifact ["+a.Id+": "+a.Name+"]");

				var b = new FabBatchNewClass();
				b.BatchId = a.Id;
				b.Name = a.Name;
				b.Disamb = a.Disamb;
				b.Note = a.Note;
				classes[i] = b;
			}

			return classes;
		}

		/*--------------------------------------------------------------------------------------------*/
		protected override FabResponse<FabBatchResult> AddToFabric(FabricClient pFabClient,
																		FabBatchNewClass[] pBatchNew) {
			return pFabClient.Services.Modify.AddClasses.Post(pBatchNew);

			/*var fr = new FabResponse<FabBatchResult>();
			fr.Data = new List<FabBatchResult>();

			foreach ( FabBatchNewClass nc in pBatchNew ) {
				var br = new FabBatchResult();
				br.BatchId = nc.BatchId;
				br.ResultId = 1;
				fr.Data.Add(br);
			}

			return fr;*/
		}

		/*--------------------------------------------------------------------------------------------*/
		protected override void SetItemTypeId(ISession pSess, Data.Domain.Export pExport, int pBatchId) {
			pExport.Artifact = pSess.Load<Artifact>(pBatchId);
			pExport.Factor = null;
		}

	}
	
}