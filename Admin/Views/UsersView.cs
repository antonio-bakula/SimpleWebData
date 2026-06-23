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
			var btnLoad = new Button { Text = Loc.T("common.refresh"), AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGray };
			var btnAddUser = new Button { Text = Loc.T("users.addUser"), AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGreen };
			var btnDelUser = new Button { Text = Loc.T("users.delUser"), AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.MistyRose };

			flowTop.Controls.Add(btnLoad);
			flowTop.Controls.Add(btnAddUser);
			flowTop.Controls.Add(btnDelUser);

			var grid = MakeGrid();
			HideTechnicalColumns(grid);
			LocalizeColumns(grid);

			async Task ReloadUsersAsync()
			{
				var users = await Api.GetAsync<List<User>>("/api/superadmin/users");
				var sites = await Api.GetAsync<List<WebSite>>("/api/superadmin/websites");

				if (!grid.Columns.Contains("SiteCombo"))
				{
					var combo = new DataGridViewComboBoxColumn { Name = "SiteCombo", HeaderText = Loc.T("col.webSite"), DataPropertyName = "WebSiteId", ValueMember = "Id", DisplayMember = "Code", DataSource = sites };
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
				{ MessageBox.Show(Loc.T("users.noSites")); return; }

				// Dijalog se gradi odmah u skaliranim mjerama (font + sve koordinate × zoom).
				int Z(int v) => UiZoom.Scaled(v);

				using (var modal = new Form
				{
					Text = Loc.T("users.dialogTitle"),
					StartPosition = FormStartPosition.CenterParent,
					FormBorderStyle = FormBorderStyle.FixedDialog,
					MaximizeBox = false,
					MinimizeBox = false,
					AutoScaleMode = AutoScaleMode.None,
					Font = UiZoom.ScaledFont(9),
					ClientSize = new Size(Z(340), Z(312))
				})
				{
					var lblUser = new Label { Text = Loc.T("users.username"), Location = new Point(Z(16), Z(16)), AutoSize = true };
					var txtUser = new TextBox { Location = new Point(Z(16), Z(42)), Width = Z(300) };

					var lblPass = new Label { Text = Loc.T("users.password"), Location = new Point(Z(16), Z(80)), AutoSize = true };
					var txtPass = new TextBox { Location = new Point(Z(16), Z(106)), Width = Z(300), PasswordChar = '*' };

					var lblSite = new Label { Text = Loc.T("users.webSite"), Location = new Point(Z(16), Z(144)), AutoSize = true };
					var cmbSite = new ComboBox { Location = new Point(Z(16), Z(170)), Width = Z(300), DataSource = sites, DisplayMember = "Code", ValueMember = "Id", DropDownStyle = ComboBoxStyle.DropDownList };

					var chkSuper = new CheckBox { Text = Loc.T("users.isSuper"), Location = new Point(Z(16), Z(212)), AutoSize = true };

					var btnOk = new Button { Text = Loc.T("common.save"), Location = new Point(Z(16), Z(250)), Width = Z(130), Height = Z(38), DialogResult = DialogResult.OK };
					var btnCancel = new Button { Text = Loc.T("common.cancel"), Location = new Point(Z(156), Z(250)), Width = Z(130), Height = Z(38), DialogResult = DialogResult.Cancel };

					modal.Controls.Add(lblUser);
					modal.Controls.Add(txtUser);
					modal.Controls.Add(lblPass);
					modal.Controls.Add(txtPass);
					modal.Controls.Add(lblSite);
					modal.Controls.Add(cmbSite);
					modal.Controls.Add(chkSuper);
					modal.Controls.Add(btnOk);
					modal.Controls.Add(btnCancel);
					modal.AcceptButton = btnOk;
					modal.CancelButton = btnCancel;

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
				if (grid.SelectedRows.Count > 0 && MessageBox.Show(Loc.T("users.delConfirm"), Loc.T("common.confirm"), MessageBoxButtons.YesNo) == DialogResult.Yes)
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
