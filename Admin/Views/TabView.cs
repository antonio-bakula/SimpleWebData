using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimpleWebDataAdmin.Services;

namespace SimpleWebDataAdmin.Views
{
	// Bazna klasa za svaki tab-view. Drži injektani ApiClient i dijeljene UI helpere.
	// MainForm lijeno učitava view preko LoadAsync() (zamjena za stari btnLoad.PerformClick()).
	public abstract class TabView : UserControl
	{
		protected ApiClient Api { get; }

		protected TabView(ApiClient api)
		{
			Api = api;
		}

		// Default je no-op (npr. ApiKey tab nema što učitavati na prikazu).
		public virtual Task LoadAsync() => Task.CompletedTask;

		// Sigurno izvođenje async akcije gumba: privremeno onemogući gumb (sprječava dvoklik /
		// re-entrancy dok poziv traje) i uhvati iznimku umjesto da sruši aplikaciju.
		protected static EventHandler OnClick(Func<Task> action) => async (sender, e) =>
		{
			var control = sender as Control;
			try
			{
				if (control != null) control.Enabled = false;
				await action();
			}
			catch (Exception ex)
			{
				ShowError(ex);
			}
			finally
			{
				if (control != null) control.Enabled = true;
			}
		};

		protected static void ShowError(Exception ex) =>
			MessageBox.Show($"{Loc.T("common.error")}: {ex.Message}", Loc.T("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);

		// Mapiranje naziva kolone (auto-generirane ili combo) -> ključ prijevoda zaglavlja.
		private static readonly Dictionary<string, string> ColumnHeaderKeys = new()
		{
			["Code"] = "col.code",
			["Name"] = "col.name",
			["Description"] = "col.description",
			["AltText"] = "col.altText",
			["FileName"] = "col.fileName",
			["Content"] = "col.content",
			["Date"] = "col.date",
			["Username"] = "col.username",
			["FirstName"] = "col.firstName",
			["LastName"] = "col.lastName",
			["Email"] = "col.email",
			["IsSuperUser"] = "col.isSuperUser",
			["GalleryCombo"] = "col.gallery",
			["StatusCombo"] = "col.status",
			["SiteCombo"] = "col.webSite",
		};

		// Prevodi zaglavlja kolona prema trenutnom jeziku. Veže se na DataBindingComplete jer se
		// auto-generirane kolone (i combo kolone) stvaraju tek pri postavljanju DataSource-a.
		protected static void LocalizeColumns(DataGridView grid)
		{
			grid.DataBindingComplete += (s, e) =>
			{
				foreach (DataGridViewColumn col in grid.Columns)
					if (ColumnHeaderKeys.TryGetValue(col.Name, out var key))
						col.HeaderText = Loc.T(key);
			};
		}

		// Standardni grid kakav koriste svi tabovi (inplace edit, full-row select, popunjava širinu).
		protected static DataGridView MakeGrid() => new()
		{
			Dock = DockStyle.Fill,
			ReadOnly = false,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			AllowUserToAddRows = false,
			BackgroundColor = Color.WhiteSmoke
		};

		// Wrappa listu (ili praznu listu ako je null) u BindingList za dvosmjerni binding grida.
		protected static BindingList<T> ToBindingList<T>(List<T>? items) => new(items ?? new List<T>());

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
