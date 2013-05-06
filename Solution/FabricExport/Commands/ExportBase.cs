using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Clients.Cs;
using Fabric.Clients.Cs.Api;
using NHibernate;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public abstract class ExportBase<T, TBatchNew> : Command 
												where T : IHasNote where TBatchNew : FabBatchNewObject {

		protected bool vDebug;

		private readonly string vTypeName;
		private readonly int vBatchSize;
		private readonly int vBatchCount;
		private readonly int vThreadCount;
		private IList<T> vItemList;
		private IList<IList<T>> vBatchList;
		private Job vJob;
		private SessionProvider vSessProv;
		private long vThreadStartTime;
		private long vThreadDoneCount;
		private long vThreadSkipCount;
		private long vFailureCount;
		private IList<FabResponse<FabBatchResult>> vThreadResults;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		protected ExportBase(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo) {
			vTypeName = typeof(T).Name;
			vBatchSize = 10;
			vBatchCount = 10;
			vThreadCount = 5;
			vDebug = false;

			if ( pMatches.Count != 4 ) {
				CommIo.Print("Invalid parameter count. Expected 3 parameters.");
				IsError = true;
				return;
			}

			int bs;
			int bc;
			int tc;
			
			if ( !int.TryParse(pMatches[1].Value, out bs) ) {
				CommIo.Print("Invalid BatchSize parameter. Expected an integer.");
				IsError = true;
				return;
			}

			if ( !int.TryParse(pMatches[2].Value, out bc) ) {
				CommIo.Print("Invalid BatchCount parameter. Expected an integer.");
				IsError = true;
				return;
			}

			if ( !int.TryParse(pMatches[3].Value, out tc) ) {
				CommIo.Print("Invalid ThreadCount parameter. Expected an integer.");
				IsError = true;
				return;
			}

			vBatchSize = bs;
			vBatchCount = bc;
			vThreadCount = tc;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public override void Start() {
			if ( IsError ) {
				CommIo.Print("Export cannot start due to error.");
				return;
			}

			vSessProv = new SessionProvider();
			//vSessProv.OutputSql = true;
			
			using ( ISession sess = vSessProv.OpenSession() ) {
				GetItems(sess);
				CreateJob(sess);
				RunThreads();
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void GetItems(ISession pSess) {
			int itemCount = vBatchSize*vBatchCount;
			CommIo.Print("Loading (up to) "+itemCount+" un-exported "+vTypeName+"s...");

			vItemList = GetWordNetItemList(pSess, itemCount);
			CommIo.Print("Found "+vItemList.Count+" "+vTypeName+"s.");

			vBatchList = new List<IList<T>>();
			int a = 0;

			for ( int i = 0 ; i < vBatchCount ; ++i ) {
				vBatchList.Add(new List<T>());
				int max = Math.Min(a+vBatchSize, vItemList.Count);

				for ( ; a < max ; ++a ) {
					vItemList[a].Note = vItemList[a].Note.Replace("&", "%26");
					vBatchList[i].Add(vItemList[a]);
				}
			}

			/*foreach ( IList<T> batch in vBatchList ) {
				CommIo.Print(" - Batch");

				foreach ( T item in batch ) {
					CommIo.Print("    * "+item.Name+" / "+item.ExportList.Count);
				}
			}*/
		}

		/*--------------------------------------------------------------------------------------------*/
		private void CreateJob(ISession pSess) {
			using ( ITransaction tx = pSess.BeginTransaction() ) {
				vJob = new Job();
				vJob.Size = vBatchSize;
				vJob.Count = vBatchCount;
				vJob.Threads = vThreadCount;
				vJob.TimeStart = DateTime.UtcNow.Ticks;
				pSess.Save(vJob);

				tx.Commit();
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void RunThreads() {
			try {
				CommIo.Print("Authenticating Fabric DataProvider...");
				var f = new FabricClient();
				f.AppDataProvSession.RefreshTokenIfNecessary();
				f.UseDataProviderPerson = true;

				if ( !f.AppDataProvSession.IsAuthenticated ) {
					throw  new Exception("DataProvider is not authenticated.");
				}

				CommIo.Print("DataProvider authenticated.");
			}
			catch ( Exception e ) {
				CommIo.Print("Authentication exception: "+e);
				return;
			}

			vThreadStartTime = DateTime.UtcNow.Ticks;
			vThreadDoneCount = 0;
			vThreadSkipCount = 0;
			vFailureCount = 0;
			vThreadResults = new List<FabResponse<FabBatchResult>>();

			////

			CommIo.Print("Starting export: count="+vBatchCount+", size="+vBatchSize+
				", threads="+vThreadCount+"...");

			var opt = new ParallelOptions();
			opt.MaxDegreeOfParallelism = vThreadCount;
			Parallel.ForEach(vBatchList, opt, ThreadAction);

			double time = (DateTime.UtcNow.Ticks-vThreadStartTime)/10000000.0;
			double perSec = ((vThreadDoneCount-vThreadSkipCount)*vBatchSize)/time;
			CommIo.Print("Export complete: "+GetSecs(vThreadStartTime)+" fab, "+
				perSec.ToString("#0.000")+" exp/sec");

			////

			CloseJob();
			CommIo.Print("Job "+vJob.Id+" complete! Failure count: "+vFailureCount);
		}

		/*--------------------------------------------------------------------------------------------*/
		private void CloseJob() {
			CommIo.Print("Saving Batch/Export results...");
			long t = DateTime.UtcNow.Ticks;
			StoreExportResults();
			CommIo.Print("Results saved in "+GetSecs(t));

			////

			CommIo.Print("Saving Job results...");
			t = DateTime.UtcNow.Ticks;

			using ( ISession sess = vSessProv.OpenSession() ) {
				using ( ITransaction tx = sess.BeginTransaction() ) {
					vJob = sess.Get<Job>(vJob.Id);
					vJob.TimeEnd = DateTime.UtcNow.Ticks;
					sess.SaveOrUpdate(vJob);

					tx.Commit();
				}
			}

			CommIo.Print("Results saved in "+GetSecs(t));
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void ThreadAction(IList<T> pBatch, ParallelLoopState pState, long pIndex) {
			if ( pBatch == null || pBatch.Count == 0 ) {
				ThreadPrint(pIndex, "Batch is empty, leaving thread");
				++vThreadDoneCount;
				++vThreadSkipCount;
				return;
			}

			try {
				long t = DateTime.UtcNow.Ticks;
				FabResponse<FabBatchResult> fr = ThreadAddItemsToFabric(pBatch, pIndex);
				string fabSecs = GetSecs(t);

				lock ( vThreadResults ) {
					vThreadResults.Add(fr);
				}

				++vThreadDoneCount;

				double perc = vThreadDoneCount/(double)vBatchCount;
				double time = (DateTime.UtcNow.Ticks-vThreadStartTime)/10000000.0;
				double perSec = ((vThreadDoneCount-vThreadSkipCount)*vBatchSize)/time;

				long bar = (DateTime.UtcNow.Ticks-t)/1000000; //tenths of a second
				string barStr = new string('#', (int)bar);

				ThreadPrint(pIndex,
					(vDebug ? " * ............................................................. " : "")+
					"Finished batch "+vThreadDoneCount+" of "+vBatchCount+" \t"+
					fabSecs+" fab \t"+
					GetSecs(t)+" thr \t"+
					GetSecs(vThreadStartTime)+" tot \t"+
					(perc*100).ToString("##0.000")+"% \t"+
					perSec.ToString("#0.000")+" exp/sec | "+barStr);
			}
			catch ( Exception e ) {
				ThreadPrint(pIndex, " # EXCEPTION: "+e);
				vFailureCount += vBatchSize;
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private FabResponse<FabBatchResult> ThreadAddItemsToFabric(IList<T> pBatch, long pIndex) {
			var f = new FabricClient();
			f.AppDataProvSession.RefreshTokenIfNecessary();
			f.UseDataProviderPerson = true;

			if ( !f.AppDataProvSession.IsAuthenticated ) {
				throw new Exception("Could not authenticate.");
			}

			if ( vDebug ) ThreadPrint(pIndex, "Starting batch...");

			TBatchNew[] newBatchItems = GetNewBatchList(pBatch, pIndex);
			FabResponse<FabBatchResult> fr = AddToFabric(f, newBatchItems);

			if ( fr == null ) {
				vFailureCount += vBatchSize;
				throw new Exception(" - FabResponse is null.");
			}

			if ( fr.Error != null ) {
				FabError e = fr.Error;
				vFailureCount += vBatchSize;
				throw new Exception(" - FabError "+e.Code+": "+e.Name+" / "+e.Message);
			}

			if ( fr.Data == null ) {
				vFailureCount += vBatchSize;
				throw new Exception(" - FabResponse.Data is null.");
			}

			foreach ( FabBatchResult br in fr.Data ) {
				if ( br.Error == null ) {
					continue;
				}

				vFailureCount++;

				CommIo.Print(" # ERROR: "+br.Error.Name+" ("+br.Error.Code+"): "+br.Error.Message+
					" ["+br.BatchId+" / "+br.ResultId+"]");

				//Enables "repair" mode
				/*if ( br.Error.Name == "UniqueConstraintViolation" ) {
					const string idStr = "ClassId=";
					string msg = br.Error.Message;
					int idIndex = msg.IndexOf(idStr);

					if ( idIndex != -1 ) {
						idIndex += idStr.Length;
						int dotIndex = msg.IndexOf(".", idIndex);
						string classId = msg.Substring(idIndex, dotIndex-idIndex);
						ThreadPrint(pIndex, "Repair: "+br.BatchId+", "+b.Id+", "+classId);

						var e2 = new Data.Domain.Export();
						e2.Batch = b;
						e2.FabricId = long.Parse(classId);
						SetItemTypeId(sess, e2, (int)br.BatchId);
						sess.Save(e2);
					}
				}*/
			}

			if ( vDebug ) {
				foreach ( FabBatchResult br in fr.Data ) {
					ThreadPrint(pIndex, " * Export success: "+vTypeName+" "+
						br.BatchId+" == Fab "+br.ResultId);
				}
			}

			return fr;
		}

		/*--------------------------------------------------------------------------------------------*/
		private void StoreExportResults() {
			using ( ISession sess = vSessProv.OpenSession() ) {
				using ( ITransaction tx = sess.BeginTransaction() ) {
					foreach ( FabResponse<FabBatchResult> fr in vThreadResults ) {

						var b = new Batch();
						b.Job = sess.Load<Job>(vJob.Id);
						b.Timestamp = fr.Timestamp;
						b.DataLen = fr.DataLen;
						b.DbMs = fr.DbMs;
						b.TotalMs = fr.TotalMs;
						sess.Save(b);

						foreach ( FabBatchResult br in fr.Data ) {
							if ( br.Error != null ) {
								continue;
							}

							var e = new Data.Domain.Export();
							e.Batch = b;
							e.FabricId = br.ResultId;
							SetItemTypeId(sess, e, (int)br.BatchId);
							sess.Save(e);
						}

					} //end "results" loop

					tx.Commit();
				}
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		protected abstract IList<T> GetWordNetItemList(ISession pSess, int pItemCount);

		/*--------------------------------------------------------------------------------------------*/
		protected abstract TBatchNew[] GetNewBatchList(IList<T> pBatch, long pIndex);

		/*--------------------------------------------------------------------------------------------*/
		protected abstract FabResponse<FabBatchResult> AddToFabric(FabricClient pFabClient,
																				TBatchNew[] pBatchNew);

		/*--------------------------------------------------------------------------------------------*/
		protected abstract void SetItemTypeId(ISession pSess, Data.Domain.Export pExport, int pBatchId);


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		protected void ThreadPrint(long pIndex, string pText) {
			CommIo.Print("T"+pIndex+": \t"+pText);
		}

		/*--------------------------------------------------------------------------------------------*/
		protected static string GetSecs(long pFromTime) {
			return ((DateTime.UtcNow.Ticks-pFromTime)/10000000.0).ToString("##0.000")+" sec";
		}

	}
	
}
