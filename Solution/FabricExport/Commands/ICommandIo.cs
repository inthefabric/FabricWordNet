using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public interface ICommandIo {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		void RequestStop();

		/*--------------------------------------------------------------------------------------------*/
		void Print(string pText);
		KeyValuePair<Commander.Command, MatchCollection>? ReadCommand();

	}
	
}