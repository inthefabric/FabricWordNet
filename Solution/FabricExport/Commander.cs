using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fabric.Apps.WordNet.Export.Commands;

namespace Fabric.Apps.WordNet.Export {

	/*================================================================================================*/
	public class Commander : ICommandIo {

		private const string Prompt = "\nFabricExporter> ";

		public enum Command {
			Export = 1,
			//StopExport
			Exit
		}

		private static Dictionary<string, Command> CommandText;

		private bool vIsRunning;
		private bool vIsStopping;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Commander() {
			CommandText = new Dictionary<string, Command>();
			CommandText.Add("export", Command.Export);
			//CommandText.Add("stopExport", Command.StopExport);
			CommandText.Add("exit", Command.Exit);

			vIsRunning = true;
			vIsStopping = false;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void Start() {
			while ( vIsRunning ) {
				KeyValuePair<Command, MatchCollection>? commData = ReadCommand();

				if ( commData == null ) {
					continue;
				}

				MatchCollection matches = commData.Value.Value;

				switch ( commData.Value.Key ) {
					case Command.Export:
						var ex = new ExportCommand(this, matches);
						ex.Start();
						break;

					/*case Command.StopExport:
						var stop = new StopExportCommand(this, matches);
						stop.Start();
						break;*/
				}

				if ( commData.Value.Key == Command.Exit ) {
					break;
				}
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		public void RequestStop() {
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