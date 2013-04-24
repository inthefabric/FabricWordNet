using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Export.Commands;

namespace Fabric.Apps.WordNet.Export {

	/*================================================================================================*/
	public class Commander : ICommandIo {

		private const string Prompt = "\nFabricExporter> ";

		public enum Command {
			ExportArt = 1,
			ExportFac,
			Artifact,
			ConfirmJob,
			ConfirmAllJobs,
			Exit
		}

		private static Dictionary<string, Command> CommandText;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Commander() {
			CommandText = new Dictionary<string, Command>();
			CommandText.Add("export-art", Command.ExportArt);
			CommandText.Add("export-fac", Command.ExportFac);
			CommandText.Add("art", Command.Artifact);
			CommandText.Add("confirmjob", Command.ConfirmJob);
			CommandText.Add("confirmalljobs", Command.ConfirmAllJobs);
			CommandText.Add("exit", Command.Exit);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void Start() {
			while ( true ) {
				KeyValuePair<Command, MatchCollection>? commData = ReadCommand();

				if ( commData == null ) {
					continue;
				}

				MatchCollection matches = commData.Value.Value;

				switch ( commData.Value.Key ) {
					case Command.ExportArt:
						var exa = new ExportArtCommand(this, matches);
						exa.Start();
						break;

					case Command.ExportFac:
						var exf = new ExportFacCommand(this, matches);
						exf.Start();
						break;

					case Command.Artifact:
						var ar = new ArtifactCommand(this, matches);
						ar.Start();
						break;

					case Command.ConfirmJob:
						var cj = new ConfirmJobCommand(this, matches);
						cj.Start();
						break;

					case Command.ConfirmAllJobs:
						var caj = new ConfirmAllJobsCommand(this, matches);
						caj.Start();
						break;
				}

				if ( commData.Value.Key == Command.Exit ) {
					break;
				}
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void Print(string pText) {
			Console.WriteLine(pText);
		}

		/*--------------------------------------------------------------------------------------------*/
		public KeyValuePair<Command, MatchCollection>? ReadCommand() {
			Console.Write(Prompt);
			string input = Console.ReadLine();
			Console.WriteLine();

			if ( string.IsNullOrEmpty(input) ) {
				return null;
			}

			MatchCollection matches = Regex.Matches(input, @"\S+");
			string inputComm = matches[0].Value;
			Command comm;

			if ( !CommandText.TryGetValue(inputComm, out comm) ) {
				Print("Unknown command: '"+inputComm+"'. Known commands: "+
					string.Join(", ", CommandText.Keys));
				return null;
			}

			return new KeyValuePair<Command, MatchCollection>(comm, matches);
		}

	}
	
}