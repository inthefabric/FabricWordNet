using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using NHibernate;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public class ArtifactCommand : Command {

		private readonly int vArtifactId;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ArtifactCommand(ICommandIo pCommIo, MatchCollection pMatches) : base(pCommIo) {
			if ( pMatches.Count != 2 ) {
				CommIo.Print("Invalid parameter count. Expected 2 parameters.");
				IsError = true;
				return;
			}

			int ai;
			
			if ( !int.TryParse(pMatches[1].Value, out ai) ) {
				CommIo.Print("Invalid ArtifactId parameter. Expected an integer.");
				IsError = true;
				return;
			}

			vArtifactId = ai;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public override void Start() {
			var sp = new SessionProvider();
			
			using ( ISession sess = sp.OpenSession() ) {
				Artifact a = sess.QueryOver<Artifact>()
					.Where(x => x.Id == vArtifactId)
					.SingleOrDefault();

				Data.Domain.Export e = sess.QueryOver<Data.Domain.Export>()
					.Where(x => x.Artifact.Id == vArtifactId)
					.Fetch(x => x.Batch).Eager
					.SingleOrDefault();

				CommIo.Print("Artifact.Id:        "+a.Id);
				CommIo.Print("Artifact.Name:      "+a.Name);
				CommIo.Print("Artifact.Note:      "+a.Note);
				CommIo.Print("Artifact.Disamb:    "+a.Disamb);
				CommIo.Print("Artifact.SynsetId:  "+(a.Synset == null ? "NULL" : a.Synset.Id+""));
				CommIo.Print("Artifact.WordId:    "+(a.Word == null ? "NULL" : a.Word.Id+""));
				CommIo.Print("");

				if ( e == null ) {
					CommIo.Print("Export:             NULL");
				}
				else {
					CommIo.Print("Export.Id:          "+e.Id);
					CommIo.Print("Export.FabricId:    "+e.FabricId);
					CommIo.Print("");

					CommIo.Print("Batch.Id:           "+e.Batch.Id);
					CommIo.Print("Batch.DbMs:         "+e.Batch.DbMs);
					CommIo.Print("Batch.TotalMs:      "+e.Batch.TotalMs);
					//CommIo.Print("Batch.Size:         "+e.Batch.Size);
					//CommIo.Print("Batch.Count:        "+e.Batch.Count);
					//CommIo.Print("Batch.Threads:      "+e.Batch.Threads);
				}
			}
		}

	}
	
}