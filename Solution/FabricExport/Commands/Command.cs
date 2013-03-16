namespace Fabric.Apps.WordNet.Export.Commands {

	/*================================================================================================*/
	public abstract class Command : ICommand {

		protected ICommandIo CommIo { get; private set; }
		protected bool IsError { get; set; }


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public Command(ICommandIo pCommIo) {
			CommIo = pCommIo;
		}

		/*--------------------------------------------------------------------------------------------*/
		public abstract void Start();
		public abstract void RequestStop();

	}
	
}