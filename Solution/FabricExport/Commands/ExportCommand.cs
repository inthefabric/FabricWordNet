using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Clients.Cs;
using Fabric.Clients.Cs.Api;
using NHibernate;
using NHibernate.SqlCommand;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public class ExportCommand : Command {

		private bool vDebug;

		private readonly int vBatchSize;
		private readonly int vBatchCount;
		private readonly int vThreadCount;
		private IList<Artifact> vArtifactList;
		private IList<IList<Artifact>> vBatchList;
		private Job vJob;
		private SessionProvider vSessProv;
		private long vThreadStartTime;
		private long vThreadDoneCount;
		private long vFailureCount;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ExportCommand(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo) {
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
				GetArtifacts(sess);
				CreateJob(sess);
				RunThreads();
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void GetArtifacts(ISession pSess) {
			int artCount = vBatchSize*vBatchCount;

			CommIo.Print("Loading (up to) "+artCount+" un-exported Artifacts...");
			Artifact artAlias = null;

			vArtifactList = pSess.QueryOver<Artifact>(() => artAlias)
				.JoinQueryOver<Data.Domain.Export>(x => x.ExportList, JoinType.LeftOuterJoin)
				.Where(x => x.Artifact == null)
				.Fetch(x => x.ExportList).Eager
				.OrderBy(() => artAlias.Id).Asc
				.Take(artCount)
				.List();

			CommIo.Print("Found "+vArtifactList.Count+" Artifacts.");

			vBatchList = new List<IList<Artifact>>();
			int a = 0;

			for ( int i = 0 ; i < vBatchCount ; ++i ) {
				vBatchList.Add(new List<Artifact>());
				int max = Math.Min(a+vBatchSize, vArtifactList.Count);

				for ( ; a < max ; ++a ) {
					vArtifactList[a].Note = vArtifactList[a].Note.Replace("&", "%26");
					vBatchList[i].Add(vArtifactList[a]);
				}
			}

			/*foreach ( IList<Artifact> batch in vBatchList ) {
				CommIo.Print(" - Batch");

				foreach ( Artifact art in batch ) {
					CommIo.Print("    * "+art.Name+" / "+art.ExportList.Count);
				}
			}*/
		}

		/*--------------------------------------------------------------------------------------------*/
		public void CreateJob(ISession pSess) {
			using ( ITransaction tx = pSess.BeginTransaction() ) {
				vJob = new Job();
				vJob.TimeStart = DateTime.UtcNow.Ticks;
				pSess.Save(vJob);

				tx.Commit();
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		public void RunThreads() {
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
			vFailureCount = 0;

			var opt = new ParallelOptions();
			opt.MaxDegreeOfParallelism = vThreadCount;

			Parallel.ForEach(vBatchList, opt, ThreadAction);
			CloseJob();
			CommIo.Print("Job "+vJob.Id+" complete! Failure count: "+vFailureCount);
		}

		/*--------------------------------------------------------------------------------------------*/
		public void CloseJob() {
			using ( ISession sess = vSessProv.OpenSession() ) {
				using ( ITransaction tx = sess.BeginTransaction() ) {
					vJob = sess.Get<Job>(vJob.Id);
					vJob.TimeEnd = DateTime.UtcNow.Ticks;
					sess.SaveOrUpdate(vJob);

					tx.Commit();
				}
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void ThreadAction(IList<Artifact> pBatch, ParallelLoopState pState, long pIndex) {
			if ( pBatch == null || pBatch.Count == 0 ) {
				ThreadPrint(pIndex, "Batch is empty, leaving thread");
				++vThreadDoneCount;
				return;
			}

			try {
				long t = DateTime.UtcNow.Ticks;

				FabResponse<FabBatchResult> fr = ThreadAddFabricClasses(pBatch, pIndex);
				ThreadAddBatchExport(fr, pIndex);

				++vThreadDoneCount;
				double perc = vThreadDoneCount/(double)vBatchCount;
				double time = (DateTime.UtcNow.Ticks-vThreadStartTime)/10000000.0;
				double perSec = (vThreadDoneCount*vBatchSize)/time;

				long bar = (DateTime.UtcNow.Ticks-t)/1000000; //tenths of a second
				string barStr = new string('#', (int)bar);

				ThreadPrint(pIndex, 
					(vDebug ? " * .............................................................. " : "")+
					"Finished batch "+vThreadDoneCount+" of "+vBatchCount+" \t"+
					GetSecs(t)+" thr \t"+
					GetSecs(vThreadStartTime)+" tot \t"+
					(perc*100).ToString("##0.000")+"% \t"+
					perSec.ToString("#0.000")+" exp/sec | "+barStr);
			}
			catch ( Exception e ) {
				ThreadPrint(pIndex, " # EXCEPTION: "+e);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private FabResponse<FabBatchResult> ThreadAddFabricClasses(IList<Artifact> pBatch, long pIndex){
			var f = new FabricClient();
			f.AppDataProvSession.RefreshTokenIfNecessary();
			f.UseDataProviderPerson = true;

			if ( !f.AppDataProvSession.IsAuthenticated ) {
				throw new Exception("Could not authenticate.");
			}

			if ( vDebug ) ThreadPrint(pIndex, "Starting batch...");
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

			//var t = DateTime.UtcNow;
			FabResponse<FabBatchResult> fr = f.Services.Modify.AddClasses.Post(classes);
			//ThreadPrint(pIndex, " ... batch time: "+(int)(DateTime.UtcNow-t).TotalMilliseconds);

			if ( fr.Error != null ) {
				FabError e = fr.Error;
				vFailureCount += vBatchSize;
				throw new Exception(" - FabError "+e.Code+": "+e.Name+" / "+e.Message);
			}

			if ( fr.Data == null ) {
				vFailureCount += vBatchSize;
				throw new Exception(" - FabResponse.Data is null.");
			}

			if ( vDebug ) {
				foreach ( FabBatchResult br in fr.Data ) {
					ThreadPrint(pIndex, " * Export success: Artifact "+
						br.BatchId+" == FabClass "+br.ResultId);
				}
			}

			return fr;
		}

		/*--------------------------------------------------------------------------------------------*/
		private void ThreadAddBatchExport(FabResponse<FabBatchResult> pFabResp, long pIndex) {
			//ThreadPrint(pIndex, " * Adding Export item to the database...");

			using ( ISession sess = vSessProv.OpenSession() ) {
				using ( ITransaction tx = sess.BeginTransaction() ) {
					var b = new Batch();
					b.Job = sess.Load<Job>(vJob.Id);
					b.Size = vBatchSize;
					b.Count = vBatchCount;
					b.Threads = vThreadCount;
					b.Timestamp = pFabResp.Timestamp;
					b.DataLen = pFabResp.DataLen;
					b.DbMs = pFabResp.DbMs;
					b.TotalMs = pFabResp.TotalMs;
					sess.Save(b);

					foreach ( FabBatchResult br in pFabResp.Data ) {
						if ( br.Error != null ) {
							vFailureCount++;
							ThreadPrint(pIndex, " # ERROR: "+br.Error.Name+
								" ("+br.Error.Code+"): "+br.Error.Message+
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
									e2.Artifact = sess.Load<Artifact>((int)br.BatchId);
									e2.Factor = null;
									sess.Save(e2);
								}
							}*/

							continue;
						}

						var e = new Data.Domain.Export();
						e.Batch = b;
						e.FabricId = br.ResultId;
						e.Artifact = sess.Load<Artifact>((int)br.BatchId);
						e.Factor = null;
						sess.Save(e);
					}

					tx.Commit();
				}
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void ThreadPrint(long pIndex, string pText) {
			CommIo.Print("T"+pIndex+": \t"+pText);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string GetSecs(long pFromTime) {
			return ((DateTime.UtcNow.Ticks-pFromTime)/10000000.0).ToString("##0.000")+" sec";
		}

	}
	
}