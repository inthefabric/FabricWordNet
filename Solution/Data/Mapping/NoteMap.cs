using Fabric.Apps.WordNet.Data.Domain;
using FluentNHibernate.Mapping;

namespace Fabric.Apps.WordNet.Data.Mapping {

	/*================================================================================================*/
	public class NoteMap : ClassMap<Note> {


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public NoteMap() {
			Id(x => x.Id)
				.Column(typeof(Note).Name+"Id")
				.GeneratedBy.Native();

			Map(x => x.Type);
			Map(x => x.Text);
		}

	}

}