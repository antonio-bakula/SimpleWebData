using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleWebDataAdmin.Views
{
	// Bazna klasa za svaki tab-view. Svaki view sam gradi svoj UI u konstruktoru,
	// a MainForm ga lijeno učitava preko LoadAsync() (zamjena za stari btnLoad.PerformClick()).
	public abstract class TabView : UserControl
	{
		// Default je no-op (npr. ApiKey tab nema što učitavati).
		public virtual Task LoadAsync() => Task.CompletedTask;

		// Sakriva tehničke (FK/Id) stupce kad se grid napuni. Dijeljeno između svih viewova.
		protected static void HideTechnicalColumns(DataGridView grid)
		{
			grid.DataBindingComplete += (s, e) =>
			{
				if (grid.Columns["Id"] != null)
					grid.Columns["Id"]!.Visible = false;

				if (grid.Columns["WebSiteId"] != null)
					grid.Columns["WebSiteId"]!.Visible = false;

				if (grid.Columns["PhotoGalleryId"] != null)
					grid.Columns["PhotoGalleryId"]!.Visible = false;

				if (grid.Columns["PageId"] != null)
					grid.Columns["PageId"]!.Visible = false;

				if (grid.Columns["FacilityId"] != null)
					grid.Columns["FacilityId"]!.Visible = false;
			};
		}
	}
}
