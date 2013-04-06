using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Clients.Cs;
using Fabric.Clients.Cs.Api;
using NHibernate;
using NHibernate.Transform;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public class ConfirmAllJobsCommand : Command {

		private readonly List<Batch> vFailList;
		private int vTotalCount;
		private int vCheckCount;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ConfirmAllJobsCommand(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo) {
			if ( pMatches.Count != 1 ) {
				CommIo.Print("Invalid parameter count. Expected 0 parameters.");
				IsError = true;
				return;
			}

			vFailList = new List<Batch>();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public override void Start() {
			try {
				CommIo.Print("Authenticating Fabric DataProvider...");
				var f = new FabricClient();
				f.AppDataProvSession.RefreshTokenIfNecessary();
				f.UseDataProviderPerson = true;

				if ( !f.AppDataProvSession.IsAuthenticated ) {
					throw new Exception("DataProvider is not authenticated.");
				}

				CommIo.Print("DataProvider authenticated.");
			}
			catch ( Exception e ) {
				CommIo.Print("Authentication exception: "+e);
				return;
			}

			var sp = new SessionProvider();
			
			IList<Batch> batchList;

			using ( ISession sess = sp.OpenSession() ) {
				CommIo.Print("Loading all Batches...");
				batchList = sess.QueryOver<Batch>()
					.Fetch(x => x.ExportList).Eager
					.TransformUsing(Transformers.DistinctRootEntity)
					.List();

				vTotalCount = batchList.Count;
				vCheckCount = 0;

				CommIo.Print("Found "+vTotalCount+" Batches");
			}

			var opt = new ParallelOptions();
			opt.MaxDegreeOfParallelism = 20;
			Parallel.ForEach(batchList, opt, CheckBatch);

			CommIo.Print("");
			CommIo.Print("Failures: "+vFailList.Count);

			foreach ( Batch b in vFailList ) {
				CommIo.Print(" - Batch "+b.Id);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		public override void RequestStop() {}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void CheckBatch(Batch pBatch, ParallelLoopState pState, long pIndex) {
			string msg = "BatchId "+pBatch.Id;

			if ( pBatch.ExportList.Count == 0 ) {
				++vCheckCount;
				CommIo.Print(msg+"Empty.");
				return;
			}

			Data.Domain.Export e = pBatch.ExportList[0];
			//CommIo.Print(" - First Export Id="+e.Id+", ArtifactId="+e.Artifact.Id+
			//			", FabricId="+e.FabricId);

			var f = new FabricClient();
			f.AppDataProvSession.RefreshTokenIfNecessary();
			f.UseDataProviderPerson = true;

			if ( !f.AppDataProvSession.IsAuthenticated ) {
				throw new Exception("Could not authenticate.");
			}

			FabResponse<FabClass> fr = 
				f.Services.Traversal.GetRootStep.ContainsClassList.WhereId(e.FabricId).Get();

			msg += " \t("+(++vCheckCount)+" \tof "+vTotalCount+"): \t";

			if ( fr == null ) {
				lock ( vFailList ) {
					vFailList.Add(pBatch);
				}

				CommIo.Print(msg+"Failed. FabResponse was null.");
				return;
			}

			FabClass c = fr.FirstDataItem();

			if ( c == null ) {
				lock ( vFailList ) {
					vFailList.Add(pBatch);
				}

				CommIo.Print(msg+"Failed. FabClass was null.");
				return;
			}

			CommIo.Print(msg+"ClassId="+c.ClassId+".");
		}

	}
	
}