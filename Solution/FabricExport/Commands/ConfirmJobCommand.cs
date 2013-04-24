using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Clients.Cs;
using Fabric.Clients.Cs.Api;
using NHibernate;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public class ConfirmJobCommand : Command {

		private int vJobId;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ConfirmJobCommand(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo) {
			if ( pMatches.Count != 1 && pMatches.Count != 2 ) {
				CommIo.Print("Invalid parameter count. Expected 1 or 2 parameters.");
				IsError = true;
				return;
			}

			if ( pMatches.Count == 1 ) {
				vJobId = -1;
				return;
			}

			int ji;
			
			if ( !int.TryParse(pMatches[1].Value, out ji) ) {
				CommIo.Print("Invalid JobId parameter. Expected an integer.");
				IsError = true;
				return;
			}

			vJobId = ji;
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
			
			using ( ISession sess = sp.OpenSession() ) {
				if ( vJobId == -1 ) {
					Job j = sess.QueryOver<Job>()
						.OrderBy(x => x.Id).Desc
						.Take(1)
						.SingleOrDefault();

					vJobId = j.Id;
				}

				CommIo.Print("Loading Job "+vJobId);

				IList<Batch> batchList = sess.QueryOver<Batch>()
					.Where(x => x.Job.Id == vJobId)
					.List();

				var failList = new List<Batch>();
				int i = 0;

				foreach ( Batch b in batchList ) {
					CommIo.Print("Confirming Batch "+b.Id+" ("+(++i)+" of "+batchList.Count+")");
					Data.Domain.Export e = b.ExportList[0];
					CommIo.Print(" - First Export Id="+e.Id+", ArtifactId="+e.Artifact.Id+
						", FabricId="+e.FabricId);

					var f = new FabricClient();
					f.AppDataProvSession.RefreshTokenIfNecessary();
					f.UseDataProviderPerson = true;

					if ( !f.AppDataProvSession.IsAuthenticated ) {
						throw new Exception("Could not authenticate.");
					}

					FabResponse<FabClass> fr = 
						f.Services.Traversal.GetRootStep.ContainsClassList.WhereId(e.FabricId).Get();

					if ( fr == null ) {
						failList.Add(b);
						CommIo.Print(" - FabResponse was null.");
						continue;
					}

					FabClass c = fr.FirstDataItem();

					if ( c == null ) {
						failList.Add(b);
						CommIo.Print(" - FabClass was null.");
						continue;
					}

					CommIo.Print(" - Found class: "+c.ClassId);
				}

				CommIo.Print("");
				CommIo.Print("Failures: "+failList.Count);

				foreach ( Batch b in failList ) {
					CommIo.Print(" - Batch "+b.Id);
				}
			}
		}

	}
	
}