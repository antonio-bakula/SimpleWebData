using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimpleWebDataAdmin.Services;
using SimpleWebDataAdmin.Models;

namespace SimpleWebDataAdmin.Views
{
	// [SuperAdmin] Upravljanje Web Site-ovima.
	public class WebSitesView : TabView
	{
		private readonly Func<Task> _load;

		public WebSitesView(ApiClient api) : base(api)
		{
			BackColor = Color.White;

			var flowTop = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
			var btnLoad = new Button { Text = "Osvježi", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGray };
			var btnAddSite = new Button { Text = "Dodaj Web Site", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGreen };
			var btnDelSite = new Button { Text = "Obriši odabrano", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.MistyRose };

			flowTop.Controls.Add(btnLoad);
			flowTop.Controls.Add(btnAddSite);
			flowTop.Controls.Add(btnDelSite);

			var grid = MakeGrid();
			HideTechnicalColumns(grid);

			async Task ReloadSitesAsync()
			{
				var sites = await Api.GetAsync<List<WebSite>>("/api/superadmin/websites");
				grid.DataSource = ToBindingList(sites);
			}

			btnLoad.Click += OnClick(() => ReloadSitesAsync());

			grid.CellEndEdit += async (s, e) =>
			{
				var w = grid.Rows[e.RowIndex].DataBoundItem as WebSite;
				if (w == null) return;
				await Api.PutAsync($"/api/superadmin/websites/{w.Id}", w);
			};

			btnAddSite.Click += OnClick(async () =>
			{
				var code = Dialogs.AskText("Novi Web Site", "Šifra (Code):", "novi-site");
				if (code == null) return;
				var name = Dialogs.AskText("Novi Web Site", "Naziv (Name):", code);
				if (name == null) return;
				await Api.PostAsync<WebSite>("/api/superadmin/websites", new WebSite { Code = code, Name = name, Description = "Novi Web Site" });
				await ReloadSitesAsync();
			});

			btnDelSite.Click += OnClick(async () =>
			{
				if (grid.SelectedRows.Count > 0 && MessageBox.Show("Obriši odabrani Web Site?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var w = grid.SelectedRows[0].DataBoundItem as WebSite;
					if (w == null) return;
					await Api.DeleteAsync($"/api/superadmin/websites/{w.Id}");
					await ReloadSitesAsync();
				}
			});

			Controls.Add(grid);
			Controls.Add(flowTop);

			_load = () => ReloadSitesAsync();
		}

		public override Task LoadAsync() => _load();
	}
}
