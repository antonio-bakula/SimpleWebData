using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimpleWebDataAdmin;
using SimpleWebDataAdmin.Models;

namespace SimpleWebDataAdmin.Views
{
	// [SuperAdmin] Upravljanje Web Site-ovima.
	public class WebSitesView : TabView
	{
		private readonly Func<Task> _load;

		public WebSitesView()
		{
			BackColor = Color.White;

			var flowTop = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
			var btnLoad = new Button { Text = "Osvježi", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGray };
			var btnAddSite = new Button { Text = "Dodaj Web Site", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGreen };
			var btnDelSite = new Button { Text = "Obriši odabrano", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.MistyRose };

			flowTop.Controls.Add(btnLoad);
			flowTop.Controls.Add(btnAddSite);
			flowTop.Controls.Add(btnDelSite);

			var grid = new DataGridView {
				Dock = DockStyle.Fill,
				ReadOnly = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				AllowUserToAddRows = false,
				BackgroundColor = Color.WhiteSmoke
			};
			HideTechnicalColumns(grid);

			async Task ReloadSitesAsync()
			{
				var sites = await AppState.Api.GetAsync<List<WebSite>>("/api/superadmin/websites");
				grid.DataSource = new System.ComponentModel.BindingList<WebSite>(sites ?? new List<WebSite>());
			}

			btnLoad.Click += async (s, e) => await ReloadSitesAsync();

			grid.CellEndEdit += async (s, e) =>
			{
				var w = grid.Rows[e.RowIndex].DataBoundItem as WebSite;
				if (w == null) return;
				await AppState.Api.PutAsync($"/api/superadmin/websites/{w.Id}", w);
			};

			btnAddSite.Click += async (s, e) =>
			{
				using (var modal = new Form { ClientSize = new Size(300, 140), Text = "Novi Web Site", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
				{
					var lbl = new Label { Text = "Šifra (Code):", Location = new Point(20, 15), AutoSize = true };
					var txtCode = new TextBox { Location = new Point(20, 40), Width = 260, Text = "novi-site" };
					var btnOk = new Button { Text = "Spremi", Location = new Point(20, 85), Width = 100, Height = 30, DialogResult = DialogResult.OK };
					modal.Controls.Add(lbl);
					modal.Controls.Add(txtCode);
					modal.Controls.Add(btnOk);
					modal.AcceptButton = btnOk;

					if (modal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtCode.Text))
					{
						var w = new WebSite { Code = txtCode.Text, Description = "Novi Web Site" };
						await AppState.Api.PostAsync<WebSite>("/api/superadmin/websites", w);
						await ReloadSitesAsync();
					}
				}
			};

			btnDelSite.Click += async (s, e) =>
			{
				if (grid.SelectedRows.Count > 0 && MessageBox.Show("Obriši odabrani Web Site?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var w = grid.SelectedRows[0].DataBoundItem as WebSite;
					if (w == null) return;
					await AppState.Api.DeleteAsync($"/api/superadmin/websites/{w.Id}");
					await ReloadSitesAsync();
				}
			};

			Controls.Add(grid);
			Controls.Add(flowTop);

			_load = () => ReloadSitesAsync();
		}

		public override Task LoadAsync() => _load();
	}
}
