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
	// [SuperAdmin] Upravljanje korisnicima i njihovom pripadnošću Web Site-u.
	public class UsersView : TabView
	{
		private readonly Func<Task> _load;

		public UsersView(ApiClient api) : base(api)
		{
			BackColor = Color.White;

			var flowTop = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
			var btnLoad = new Button { Text = "Osvježi", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGray };
			var btnAddUser = new Button { Text = "Dodaj Korisnika", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGreen };
			var btnDelUser = new Button { Text = "Obriši Korisnika", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.MistyRose };

			flowTop.Controls.Add(btnLoad);
			flowTop.Controls.Add(btnAddUser);
			flowTop.Controls.Add(btnDelUser);

			var grid = MakeGrid();
			HideTechnicalColumns(grid);

			async Task ReloadUsersAsync()
			{
				var users = await Api.GetAsync<List<User>>("/api/superadmin/users");
				var sites = await Api.GetAsync<List<WebSite>>("/api/superadmin/websites");

				if (!grid.Columns.Contains("SiteCombo"))
				{
					var combo = new DataGridViewComboBoxColumn { Name = "SiteCombo", HeaderText = "Web Site (Code)", DataPropertyName = "WebSiteId", ValueMember = "Id", DisplayMember = "Code", DataSource = sites };
					grid.Columns.Add(combo);
				}
				else
				{
					(grid.Columns["SiteCombo"] as DataGridViewComboBoxColumn)!.DataSource = sites;
				}

				grid.DataSource = ToBindingList(users);
			}

			btnLoad.Click += OnClick(() => ReloadUsersAsync());

			grid.CellEndEdit += async (s, e) =>
			{
				var u = grid.Rows[e.RowIndex].DataBoundItem as User;
				if (u == null) return;
				await Api.PutAsync($"/api/superadmin/users/{u.Id}", u);
			};

			btnAddUser.Click += OnClick(async () =>
			{
				var sites = await Api.GetAsync<List<WebSite>>("/api/superadmin/websites");
				if (sites == null || sites.Count == 0)
				{ MessageBox.Show("Nema kreiranih Web Site-ova!"); return; }

				using (var modal = new Form { ClientSize = new Size(300, 300), Text = "Novi Korisnik", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
				{
					var lblUser = new Label { Text = "Korisničko Ime:", Location = new Point(20, 15), AutoSize = true };
					var txtUser = new TextBox { Location = new Point(20, 40), Width = 260 };

					var lblPass = new Label { Text = "Lozinka (Password):", Location = new Point(20, 75), AutoSize = true };
					var txtPass = new TextBox { Location = new Point(20, 100), Width = 260, PasswordChar = '*' };

					var lblSite = new Label { Text = "Web Site:", Location = new Point(20, 135), AutoSize = true };
					var cmbSite = new ComboBox { Location = new Point(20, 160), Width = 260, DataSource = sites, DisplayMember = "Code", ValueMember = "Id", DropDownStyle = ComboBoxStyle.DropDownList };

					var chkSuper = new CheckBox { Text = "Je SuperUser?", Location = new Point(20, 200), AutoSize = true };

					var btnOk = new Button { Text = "Spremi", Location = new Point(20, 240), Width = 100, Height = 30, DialogResult = DialogResult.OK };

					modal.Controls.Add(lblUser);
					modal.Controls.Add(txtUser);
					modal.Controls.Add(lblPass);
					modal.Controls.Add(txtPass);
					modal.Controls.Add(lblSite);
					modal.Controls.Add(cmbSite);
					modal.Controls.Add(chkSuper);
					modal.Controls.Add(btnOk);
					modal.AcceptButton = btnOk;

					UiZoom.ScaleForm(modal);

					if (modal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtUser.Text))
					{
						var payload = new
						{
							Username = txtUser.Text,
							Password = string.IsNullOrWhiteSpace(txtPass.Text) ? "Simple1234!" : txtPass.Text,
							WebSiteId = (int)cmbSite.SelectedValue!,
							IsSuperUser = chkSuper.Checked
						};
						await Api.PostAsync<object>("/api/superadmin/users", payload);
						await ReloadUsersAsync();
					}
				}
			});

			btnDelUser.Click += OnClick(async () =>
			{
				if (grid.SelectedRows.Count > 0 && MessageBox.Show("Obriši odabranog korisnika?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var u = grid.SelectedRows[0].DataBoundItem as User;
					if (u == null) return;
					await Api.DeleteAsync($"/api/superadmin/users/{u.Id}");
					await ReloadUsersAsync();
				}
			});

			Controls.Add(grid);
			Controls.Add(flowTop);

			_load = () => ReloadUsersAsync();
		}

		public override Task LoadAsync() => _load();
	}
}
