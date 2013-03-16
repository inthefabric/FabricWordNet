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

		private readonly int vArtCount;
		private readonly int vThreadCount;
		private IList<Artifact> vArtifactList;
		private SessionProvider vSessProv;
		private long vThreadStartTime;
		private long vThreadDoneCount;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ExportCommand(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo) {
			vArtCount = 10;
			vThreadCount = 3;

			if ( pMatches.Count > 3 ) {
				CommIo.Print("Invalid parameter count. Expected <= 2 parameters.");
				IsError = true;
				return;
			}

			if ( pMatches.Count >= 2 ) {
				int ac;
				
				if ( !int.TryParse(pMatches[1].Value, out ac) ) {
					CommIo.Print("Invalid ArtCount parameter. Expected an integer.");
					IsError = true;
					return;
				}

				vArtCount = ac;
			}

			if ( pMatches.Count == 3 ) {
				int tc;

				if ( !int.TryParse(pMatches[2].Value, out tc) ) {
					CommIo.Print("Invalid ThreadCount parameter. Expected an integer.");
					IsError = true;
					return;
				}

				vThreadCount = tc;
			}
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
				RunThreads();
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		public override void RequestStop() {
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void GetArtifacts(ISession pSess) {
			CommIo.Print("Loading (up to) "+vArtCount+" un-exported Artifacts...");
			Artifact artAlias = null;

			vArtifactList = pSess.QueryOver<Artifact>(() => artAlias)
				.JoinQueryOver<Data.Domain.Export>(x => x.ExportList, JoinType.LeftOuterJoin)
				.Where(x => x.Artifact == null)
				.Fetch(x => x.ExportList).Eager
				.OrderBy(() => artAlias.Id).Asc
				.Take(vArtCount)
				.List();

			CommIo.Print("Found "+vArtifactList.Count+" Artifacts.");

			/*foreach ( Artifact a in vArtifactList ) {
				CommIo.Print(" - "+a.Name+" / "+a.ExportList.Count);
			}*/
		}

		/*--------------------------------------------------------------------------------------------*/
		public void RunThreads() {
			vThreadStartTime = DateTime.UtcNow.Ticks;
			vThreadDoneCount = 0;
			Parallel.ForEach(vArtifactList, ThreadAction);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void ThreadAction(Artifact pArt, ParallelLoopState pState, long pIndex) {
			try {
				FabResponse<FabClass> fr = ThreadAddFabricClass(pArt, pIndex);
				FabClass c = fr.FirstDataItem();
				ThreadPrint(pIndex, "Export success: Artifact "+pArt.Id+" == FabClass "+c.ClassId);

				ThreadAddExport(pArt, fr, c, pIndex);

				++vThreadDoneCount;
				double perc = vThreadDoneCount/(double)vArtCount;
				double time = (DateTime.UtcNow.Ticks-vThreadStartTime)/10000000.0;
				double perSec = vThreadDoneCount/time;
				ThreadPrint(pIndex, "--- Done! T="+GetSecs(vThreadStartTime)+" / "+
					(perc*100)+"% done / "+perSec+" exp/sec ---");
			}
			catch ( Exception e ) {
				ThreadPrint(pIndex, "EXCEPTION: "+e);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private FabResponse<FabClass> ThreadAddFabricClass(Artifact pArt, long pIndex) {
			ThreadPrint(pIndex, "Exporting Artifact "+pArt.Id+" / "+pArt.Name+" to Fabric...");

			var f = new FabricClient();
			f.AppDataProvSession.RefreshTokenIfNecessary();
			f.UseDataProviderPerson = true;
			ThreadPrint(pIndex, "FabricClient authenticated.");

			if ( !f.AppDataProvSession.IsAuthenticated ) {
				throw new Exception("Could not authenticate.");
			}

			FabResponse<FabClass> fr = 
				f.Services.Modify.AddClass.Post(pArt.Name, pArt.Disamb, pArt.Note);

			if ( fr.Error != null ) {
				FabError e = fr.Error;
				throw new Exception("FabError "+e.Code+": "+e.Name+" / "+e.Message);
			}

			if ( fr.Data == null ) {
				throw new Exception("FabResponse.Data is null.");
			}

			return fr;
		}

		/*--------------------------------------------------------------------------------------------*/
		private void ThreadAddExport(Artifact pArt, FabResponse<FabClass> pFabResp,
																		FabClass pClass, long pIndex) {
			ThreadPrint(pIndex, "Adding Export item to the database...");

			using ( ISession sess = vSessProv.OpenSession() ) {
				using ( ITransaction tx = sess.BeginTransaction() ) {
					var e = new Data.Domain.Export();
					e.FabricId = pClass.ClassId;
					e.Artifact = pArt;
					e.Factor = null;

					e.Timestamp = pFabResp.Timestamp;
					e.DataLen = pFabResp.DataLen;
					e.DbMs = pFabResp.DbMs;
					e.TotalMs = pFabResp.TotalMs;
					sess.Save(e);

					tx.Commit();
				}
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void ThreadPrint(long pIndex, string pText) {
			CommIo.Print("Thread "+pIndex+": "+pText);
		}

		/*--------------------------------------------------------------------------------------------*/
		private static string GetSecs(long pFromTime) {
			long milli = (DateTime.UtcNow.Ticks-pFromTime)/10000;
			return (milli/1000.0)+" sec";
		}

	}
	
}