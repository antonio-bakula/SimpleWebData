using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using SimpleWebDataAdmin.Models;

namespace SimpleWebDataAdmin.Forms
{
	public class WindowSettings
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public FormWindowState WindowState { get; set; }
	}

	public class MainForm : Form
	{
		private TabControl mainTabControl = null!;
		// Pamtimo reference gumbiju za učitavanje kako bi ih pozvali kod starta
		private List<Button> loadButtons = new();

		public MainForm()
		{
			InitializeComponent();
			SetupUI();
		}

		private void InitializeComponent()
		{
			this.Text = "SimpleWebData Administrator";
			this.Size = new Size(1100, 800);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Font = new Font("Segoe UI", 9F);
			this.AutoScaleMode = AutoScaleMode.Dpi;

			mainTabControl = new TabControl
			{
				Dock = DockStyle.Fill,
				Padding = new Point(15, 8)
			};
			this.Controls.Add(mainTabControl);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			try
			{
				if (File.Exists("settings.json"))
				{
					var json = File.ReadAllText("settings.json");
					var s = JsonSerializer.Deserialize<WindowSettings>(json);
					if (s != null)
					{
						this.StartPosition = FormStartPosition.Manual;
						this.Location = new Point(s.X, s.Y);
						this.Size = new Size(s.Width, s.Height);
						this.WindowState = s.WindowState;
					}
				}
			}
			catch { /* Utišano */ }
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// Okida učitavanje po inicijalnom prikazu MainForme za sve tabove koji su loadani (ovo se može ograničiti na SelectedTab da ne spama)
			foreach (var btn in loadButtons)
				btn.PerformClick();
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			try
			{
				var s = new WindowSettings { WindowState = this.WindowState };
				if (this.WindowState == FormWindowState.Normal)
				{
					s.X = this.Location.X;
					s.Y = this.Location.Y;
					s.Width = this.Size.Width;
					s.Height = this.Size.Height;
				}
				else
				{
					s.X = this.RestoreBounds.X;
					s.Y = this.RestoreBounds.Y;
					s.Width = this.RestoreBounds.Width;
					s.Height = this.RestoreBounds.Height;
				}
				File.WriteAllText("settings.json", JsonSerializer.Serialize(s));
			}
			catch { /* Utišano */ }
		}

		private void SetupUI()
		{
			bool isSuperAdmin = AppState.Api.CurrentUser.IsSuperUser;

			if (isSuperAdmin)
			{
				this.Text = "Super Admin - SimpleWebData";
			}
			else
			{
				this.Text = $"Dobro došao {AppState.Api.CurrentUser.Username}";
			}

			// TENANT MODULI
			mainTabControl.TabPages.Add(CreateGalleryTab());
			mainTabControl.TabPages.Add(CreateFacilitiesTab());
			mainTabControl.TabPages.Add(CreatePagesTab());
			mainTabControl.TabPages.Add(CreateApiKeyTab());

			// SUPER ADMIN MODULI
			if (isSuperAdmin)
			{
				mainTabControl.TabPages.Add(CreateWebSitesTab());
				mainTabControl.TabPages.Add(CreateUsersTab());
			}

			// Automatski trigger kad korisnik prebaci na neki tab 
			mainTabControl.SelectedIndexChanged += (s, e) =>
			{
				if (mainTabControl.SelectedTab?.Tag is Button btnLoad)
				{
					btnLoad.PerformClick();
				}
			};
		}

		private TabPage CreateWebSitesTab()
		{
			var tab = new TabPage("[SA] Web Sites");
			tab.BackColor = Color.White;

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

			btnLoad.Click += async (s, e) =>
			{
				var sites = await AppState.Api.GetAsync<List<WebSite>>("/api/superadmin/websites");
				grid.DataSource = new System.ComponentModel.BindingList<WebSite>(sites ?? new List<WebSite>());
			};

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
						btnLoad.PerformClick();
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
					btnLoad.PerformClick();
				}
			};

			tab.Controls.Add(grid);
			tab.Controls.Add(flowTop);

			loadButtons.Add(btnLoad);
			tab.Tag = btnLoad;
			return tab;
		}

		private TabPage CreateUsersTab()
		{
			var tab = new TabPage("[SA] Korisnici");
			tab.BackColor = Color.White;

			var flowTop = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
			var btnLoad = new Button { Text = "Osvježi", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGray };
			var btnAddUser = new Button { Text = "Dodaj Korisnika", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGreen };
			var btnDelUser = new Button { Text = "Obriši Korisnika", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.MistyRose };

			flowTop.Controls.Add(btnLoad);
			flowTop.Controls.Add(btnAddUser);
			flowTop.Controls.Add(btnDelUser);

			var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, BackgroundColor = Color.WhiteSmoke };
			HideTechnicalColumns(grid);

			btnLoad.Click += async (s, e) =>
			{
				var users = await AppState.Api.GetAsync<List<User>>("/api/superadmin/users");
				var sites = await AppState.Api.GetAsync<List<WebSite>>("/api/superadmin/websites");

				if (!grid.Columns.Contains("SiteCombo"))
				{
					var combo = new DataGridViewComboBoxColumn { Name = "SiteCombo", HeaderText = "Web Site (Code)", DataPropertyName = "WebSiteId", ValueMember = "Id", DisplayMember = "Code", DataSource = sites };
					grid.Columns.Add(combo);
				}
				else
				{
					(grid.Columns["SiteCombo"] as DataGridViewComboBoxColumn)!.DataSource = sites;
				}

				grid.DataSource = new System.ComponentModel.BindingList<User>(users ?? new List<User>());
			};

			grid.CellEndEdit += async (s, e) =>
			{
				var u = grid.Rows[e.RowIndex].DataBoundItem as User;
				if (u == null) return;
				await AppState.Api.PutAsync($"/api/superadmin/users/{u.Id}", u);
			};

			btnAddUser.Click += async (s, e) =>
			{
				var sites = await AppState.Api.GetAsync<List<WebSite>>("/api/superadmin/websites");
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

					if (modal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtUser.Text))
					{
						var payload = new
						{
							Username = txtUser.Text,
							Password = string.IsNullOrWhiteSpace(txtPass.Text) ? "Simple1234!" : txtPass.Text,
							WebSiteId = (int)cmbSite.SelectedValue!,
							IsSuperUser = chkSuper.Checked
						};
						await AppState.Api.PostAsync<object>("/api/superadmin/users", payload);
						btnLoad.PerformClick();
					}
				}
			};

			btnDelUser.Click += async (s, e) =>
			{
				if (grid.SelectedRows.Count > 0 && MessageBox.Show("Obriši odabranog korisnika?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var u = grid.SelectedRows[0].DataBoundItem as User;
					if (u == null) return;
					await AppState.Api.DeleteAsync($"/api/superadmin/users/{u.Id}");
					btnLoad.PerformClick();
				}
			};

			tab.Controls.Add(grid);
			tab.Controls.Add(flowTop);

			loadButtons.Add(btnLoad);
			tab.Tag = btnLoad;

			return tab;
		}

		private void HideTechnicalColumns(DataGridView grid)
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


		private TabPage CreatePagesTab()
		{
			var tab = new TabPage("Stranice web-a");
			tab.BackColor = Color.White;

			var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
			split.Resize += (s, e) => { split.SplitterDistance = split.Width / 3; };

			// --- LJEVI DIO: Stranice ---
			var gLeft = new GroupBox { Text = "1. Stranice", Dock = DockStyle.Fill, Padding = new Padding(10) };
			var flowLeft = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
			var btnLoad = new Button { Text = "Osvježi", AutoSize = true, MinimumSize = new Size(80, 40), Margin = new Padding(2), BackColor = Color.LightGray };
			var btnAddPage = new Button { Text = "Dodaj", AutoSize = true, MinimumSize = new Size(80, 40), Margin = new Padding(2), BackColor = Color.LightGreen };
			var btnDelPage = new Button { Text = "Obriši", AutoSize = true, MinimumSize = new Size(80, 40), Margin = new Padding(2), BackColor = Color.MistyRose };
			flowLeft.Controls.Add(btnLoad);
			flowLeft.Controls.Add(btnAddPage);
			flowLeft.Controls.Add(btnDelPage);

			var gridPages = new DataGridView { Dock = DockStyle.Fill, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, BackgroundColor = Color.WhiteSmoke };
			HideTechnicalColumns(gridPages);
			gLeft.Controls.Add(gridPages);
			gLeft.Controls.Add(flowLeft);

			// --- DESNI DIO: Tekstovi ---
			var gRight = new GroupBox { Text = "2. Tekstovi označene stranice", Dock = DockStyle.Fill, Padding = new Padding(10) };
			var flowRight = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
			var btnAddText = new Button { Text = "Dodaj Tekst", AutoSize = true, MinimumSize = new Size(100, 40), Margin = new Padding(5), BackColor = Color.LightGreen, Enabled = false };
			var btnDelText = new Button { Text = "Obriši Tekst", AutoSize = true, MinimumSize = new Size(100, 40), Margin = new Padding(5), BackColor = Color.MistyRose, Enabled = false };
			flowRight.Controls.Add(btnAddText);
			flowRight.Controls.Add(btnDelText);

			var gridTexts = new DataGridView { Dock = DockStyle.Fill, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, BackgroundColor = Color.WhiteSmoke };
			HideTechnicalColumns(gridTexts);
			gRight.Controls.Add(gridTexts);
			gRight.Controls.Add(flowRight);

			split.Panel1.Controls.Add(gLeft);
			split.Panel2.Controls.Add(gRight);

			// LOADER
			btnLoad.Click += async (s, e) =>
			{
				var pages = await AppState.Api.GetAsync<List<Page>>("/api/admin/pages");
				var gals = await AppState.Api.GetAsync<List<PhotoGallery>>("/api/admin/photogalleries");

				if (!gridPages.Columns.Contains("GalleryCombo"))
				{
					var combo = new DataGridViewComboBoxColumn { Name = "GalleryCombo", HeaderText = "Galerija (Code)", DataPropertyName = "PhotoGalleryId", ValueMember = "Id", DisplayMember = "Code", DataSource = gals };
					gridPages.Columns.Add(combo);
				}
				else
				{
					(gridPages.Columns["GalleryCombo"] as DataGridViewComboBoxColumn)!.DataSource = gals;
				}

				gridPages.DataSource = new System.ComponentModel.BindingList<Page>(pages ?? new List<Page>());
			};

			// SELECTION CHANGED
			gridPages.SelectionChanged += async (s, e) =>
			{
				if (gridPages.SelectedRows.Count > 0 && gridPages.DataSource != null)
				{
					var p = gridPages.SelectedRows[0].DataBoundItem as Page;
					if (p != null)
					{
						var texts = await AppState.Api.GetAsync<List<PageText>>($"/api/admin/pages/{p.Id}/texts");
						gridTexts.DataSource = new System.ComponentModel.BindingList<PageText>(texts ?? new List<PageText>());
						btnAddText.Enabled = btnDelText.Enabled = true;
					}
				}
				else
				{
					gridTexts.DataSource = null;
					btnAddText.Enabled = btnDelText.Enabled = false;
				}
			};

			// IN-PLACE EDIT
			gridPages.CellEndEdit += async (s, e) =>
			{
				var p = gridPages.Rows[e.RowIndex].DataBoundItem as Page;
				if (p == null) return;
				await AppState.Api.PutAsync($"/api/admin/pages/{p.Id}", p);
			};

			gridTexts.CellEndEdit += async (s, e) =>
			{
				var t = gridTexts.Rows[e.RowIndex].DataBoundItem as PageText;
				if (t == null) return;
				await AppState.Api.PutAsync($"/api/admin/pagetexts/{t.Id}", t);
			};

			// BRISANJE I DODAVANJE STRANICA
			btnDelPage.Click += async (s, e) =>
			{
				if (gridPages.SelectedRows.Count > 0 && MessageBox.Show("Obrisati stranicu?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var p = gridPages.SelectedRows[0].DataBoundItem as Page;
					if (p == null) return;
					await AppState.Api.DeleteAsync($"/api/admin/pages/{p.Id}");
					btnLoad.PerformClick();
				}
			};

			btnAddPage.Click += async (s, e) =>
			{
				using (var modal = new Form { ClientSize = new Size(300, 140), Text = "Nova Stranica", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
				{
					var lbl = new Label { Text = "Šifra Stranice (Code):", Location = new Point(20, 15), AutoSize = true };
					var txtCode = new TextBox { Location = new Point(20, 40), Width = 260, Text = "nova-stranica" };
					var btnOk = new Button { Text = "Spremi", Location = new Point(20, 85), Width = 100, Height = 30, DialogResult = DialogResult.OK };
					modal.Controls.Add(lbl);
					modal.Controls.Add(txtCode);
					modal.Controls.Add(btnOk);
					modal.AcceptButton = btnOk;

					if (modal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtCode.Text))
					{
						var p = new Page { Code = txtCode.Text };
						await AppState.Api.PostAsync<Page>("/api/admin/pages", p);
						btnLoad.PerformClick();
					}
				}
			};

			// BRISANJE I DODAVANJE TEKSTOVA
			btnDelText.Click += async (s, e) =>
			{
				if (gridTexts.SelectedRows.Count > 0 && MessageBox.Show("Obrisati tekst?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var t = gridTexts.SelectedRows[0].DataBoundItem as PageText;
					if (t == null) return;
					await AppState.Api.DeleteAsync($"/api/admin/pagetexts/{t.Id}");

					var p = gridPages.SelectedRows[0].DataBoundItem as Page;
					if (p == null) return;
					var texts = await AppState.Api.GetAsync<List<PageText>>($"/api/admin/pages/{p.Id}/texts");
					gridTexts.DataSource = new System.ComponentModel.BindingList<PageText>(texts ?? new List<PageText>());
				}
			};

			btnAddText.Click += async (s, e) =>
			{
				var p = gridPages.SelectedRows[0].DataBoundItem as Page;
				if (p == null) return;
				using (var modal = new Form { ClientSize = new Size(400, 300), Text = "Novi Tekst", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
				{
					var lblCode = new Label { Text = "Šifra tekst bloka (Code) (Paziti da je unikatna):", Location = new Point(20, 15), AutoSize = true };
					var txtCode = new TextBox { Location = new Point(20, 40), Width = 360, Text = "blok-vazno" };

					var lblContent = new Label { Text = "Sadržaj (Content):", Location = new Point(20, 75), AutoSize = true };
					var txtContent = new TextBox { Location = new Point(20, 100), Width = 360, Height = 140, Multiline = true, ScrollBars = ScrollBars.Vertical };

					var btnOk = new Button { Text = "Spremi", Location = new Point(20, 255), Width = 100, Height = 30, DialogResult = DialogResult.OK };

					modal.Controls.Add(lblCode);
					modal.Controls.Add(txtCode);
					modal.Controls.Add(lblContent);
					modal.Controls.Add(txtContent);
					modal.Controls.Add(btnOk);
					modal.AcceptButton = btnOk;

					if (modal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtCode.Text))
					{
						var t = new PageText { Code = txtCode.Text, Content = txtContent.Text };
						await AppState.Api.PostAsync<PageText>($"/api/admin/pages/{p.Id}/texts", t);

						var texts = await AppState.Api.GetAsync<List<PageText>>($"/api/admin/pages/{p.Id}/texts");
						gridTexts.DataSource = new System.ComponentModel.BindingList<PageText>(texts ?? new List<PageText>());
					}
				}
			};

			tab.Controls.Add(split);
			loadButtons.Add(btnLoad);
			tab.Tag = btnLoad;
			return tab;
		}

		private TabPage CreateFacilitiesTab()
		{
			var tab = new TabPage("Objekti (Facilities)");
			tab.BackColor = Color.White;

			var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
			split.Resize += (s, e) => { split.SplitterDistance = (int)(split.Width * 0.66); };

			// --- LJEVI DIO: Objekti ---
			var gLeft = new GroupBox { Text = "1. Objekti", Dock = DockStyle.Fill, Padding = new Padding(10) };
			var flowLeft = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
			var btnLoad = new Button { Text = "Osvježi", AutoSize = true, MinimumSize = new Size(100, 40), Margin = new Padding(2), BackColor = Color.LightGray };
			var btnAddFac = new Button { Text = "Dodaj", AutoSize = true, MinimumSize = new Size(80, 40), Margin = new Padding(2), BackColor = Color.LightGreen };
			var btnDelFac = new Button { Text = "Obriši", AutoSize = true, MinimumSize = new Size(80, 40), Margin = new Padding(2), BackColor = Color.MistyRose };
			flowLeft.Controls.Add(btnLoad);
			flowLeft.Controls.Add(btnAddFac);
			flowLeft.Controls.Add(btnDelFac);

			var gridFac = new DataGridView { Dock = DockStyle.Fill, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, BackgroundColor = Color.WhiteSmoke };
			HideTechnicalColumns(gridFac);
			gLeft.Controls.Add(gridFac);
			gLeft.Controls.Add(flowLeft);

			// --- DESNI DIO: Datumi i Zauzetosti ---
			var gRight = new GroupBox { Text = "2. Zauzetost odabranog objekta", Dock = DockStyle.Fill, Padding = new Padding(10) };
			var flowRight = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
			var btnAddDates = new Button { Text = "Dodaj Raspon Datuma", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGreen, Enabled = false };
			var btnDelDate = new Button { Text = "Obriši Datum", AutoSize = true, MinimumSize = new Size(100, 40), Margin = new Padding(5), BackColor = Color.MistyRose, Enabled = false };
			flowRight.Controls.Add(btnAddDates);
			flowRight.Controls.Add(btnDelDate);

			var gridRes = new DataGridView { Dock = DockStyle.Fill, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, BackgroundColor = Color.WhiteSmoke };
			HideTechnicalColumns(gridRes);
			gRight.Controls.Add(gridRes);
			gRight.Controls.Add(flowRight);

			split.Panel1.Controls.Add(gLeft);
			split.Panel2.Controls.Add(gRight);

			// UI POSTAVKE
			gridFac.DataBindingComplete += (s, e) =>
			{
				if (gridFac.Columns["PhotoGalleryId"] != null)
					gridFac.Columns["PhotoGalleryId"]!.Visible = false;
				if (gridFac.Columns["Status"] != null)
					gridFac.Columns["Status"]!.Visible = false;
			};
			gridRes.DataBindingComplete += (s, e) =>
			{
				if (gridRes.Columns["Status"] != null)
					gridRes.Columns["Status"]!.Visible = false;
				if (gridRes.Columns["FacilityId"] != null)
					gridRes.Columns["FacilityId"]!.Visible = false;
			};

			// LOADER
			btnLoad.Click += async (s, e) =>
			{
				var facs = await AppState.Api.GetAsync<List<Facility>>("/api/admin/facilities");
				var gals = await AppState.Api.GetAsync<List<PhotoGallery>>("/api/admin/photogalleries");

				if (!gridFac.Columns.Contains("GalleryCombo"))
				{
					var combo = new DataGridViewComboBoxColumn { Name = "GalleryCombo", HeaderText = "Galerija (Code)", DataPropertyName = "PhotoGalleryId", ValueMember = "Id", DisplayMember = "Code", DataSource = gals };
					gridFac.Columns.Add(combo);
				}
				else
				{
					(gridFac.Columns["GalleryCombo"] as DataGridViewComboBoxColumn)!.DataSource = gals;
				}

				gridFac.DataSource = new System.ComponentModel.BindingList<Facility>(facs ?? new List<Facility>());
			};

			// Učitaj rezervacije prelaskom miša:
			gridFac.SelectionChanged += async (s, e) =>
			{
				if (gridFac.SelectedRows.Count > 0 && gridFac.DataSource != null)
				{
					var f = gridFac.SelectedRows[0].DataBoundItem as Facility;
					if (f != null)
					{
						var reservations = await AppState.Api.GetAsync<List<Reservation>>($"/api/admin/facilities/{f.Id}/reservations");

						if (!gridRes.Columns.Contains("StatusCombo"))
						{
							var comboStatus = new DataGridViewComboBoxColumn { Name = "StatusCombo", HeaderText = "Status", DataPropertyName = "Status", DataSource = Enum.GetValues(typeof(ReservationStatus)) };
							gridRes.Columns.Add(comboStatus);
						}
						gridRes.DataSource = new System.ComponentModel.BindingList<Reservation>(reservations?.OrderBy(x => x.Date).ToList() ?? new List<Reservation>());
						btnAddDates.Enabled = btnDelDate.Enabled = true;

						// Onemogući edit na samom datumu, korisnik uređuje samo Status ili dodaje/briše datume
						if (gridRes.Columns["Date"] != null)
							gridRes.Columns["Date"]!.ReadOnly = true;
					}
				}
				else
				{
					gridRes.DataSource = null;
					btnAddDates.Enabled = btnDelDate.Enabled = false;
				}
			};

			// IZMJENE (In-place edit)
			gridFac.CellEndEdit += async (s, e) =>
			{
				var f = gridFac.Rows[e.RowIndex].DataBoundItem as Facility;
				if (f == null) return;
				await AppState.Api.PutAsync($"/api/admin/facilities/{f.Id}", f);
			};

			gridRes.CellEndEdit += async (s, e) =>
			{
				var r = gridRes.Rows[e.RowIndex].DataBoundItem as Reservation;
				if (r == null) return;
				await AppState.Api.PutAsync($"/api/admin/reservations/{r.Id}", r);
			};

			// BRISANJE I DODAVANJE OBJEKATA
			btnDelFac.Click += async (s, e) =>
			{
				if (gridFac.SelectedRows.Count > 0 && MessageBox.Show("Obrisati objekt?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var f = gridFac.SelectedRows[0].DataBoundItem as Facility;
					if (f == null) return;
					await AppState.Api.DeleteAsync($"/api/admin/facilities/{f.Id}");
					btnLoad.PerformClick();
				}
			};

			btnAddFac.Click += async (s, e) =>
			{
				using (var fModal = new Form { Width = 300, Height = 170, Text = "Novi Objekt", StartPosition = FormStartPosition.CenterParent })
				{
					var lbl = new Label { Text = "Šifra Objekta (Code):", Location = new Point(20, 10), AutoSize = true };
					var txtCode = new TextBox { Location = new Point(20, 30), Width = 200, Text = "novi-objekt" };
					var btnOk = new Button { Text = "Spremi", Location = new Point(20, 70), DialogResult = DialogResult.OK };
					fModal.Controls.Add(lbl);
					fModal.Controls.Add(txtCode);
					fModal.Controls.Add(btnOk);
					fModal.AcceptButton = btnOk;

					if (fModal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtCode.Text))
					{
						var f = new Facility { Code = txtCode.Text, Name = "Novi Objekt" };
						await AppState.Api.PostAsync<Facility>("/api/admin/facilities", f);
						btnLoad.PerformClick();
					}
				}
			};

			btnDelDate.Click += async (s, e) =>
			{
				if (gridRes.SelectedRows.Count > 0 && MessageBox.Show("Obrisati zapis datuma?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var r = gridRes.SelectedRows[0].DataBoundItem as Reservation;
					if (r == null) return;
					await AppState.Api.DeleteAsync($"/api/admin/reservations/{r.Id}");

					// Osvježi grid rezervacija
					var f = gridFac.SelectedRows[0].DataBoundItem as Facility;
					if (f == null) return;
					var reservations = await AppState.Api.GetAsync<List<Reservation>>($"/api/admin/facilities/{f.Id}/reservations");
					gridRes.DataSource = new System.ComponentModel.BindingList<Reservation>(reservations?.OrderBy(x => x.Date).ToList() ?? new List<Reservation>());
				}
			};

			btnAddDates.Click += async (s, e) =>
			{
				var f = gridFac.SelectedRows[0].DataBoundItem as Facility;
				if (f == null) return;
				using (var modal = new Form { ClientSize = new Size(250, 240), Text = "Dodaj Datume", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
				{
					var lblOd = new Label { Text = "Od datuma:", Location = new Point(20, 15), AutoSize = true };
					var dtpOd = new DateTimePicker { Location = new Point(20, 40), Width = 180, Format = DateTimePickerFormat.Short };

					var lblDo = new Label { Text = "Do datuma:", Location = new Point(20, 75), AutoSize = true };
					var dtpDo = new DateTimePicker { Location = new Point(20, 100), Width = 180, Format = DateTimePickerFormat.Short };

					var lblStatus = new Label { Text = "Status:", Location = new Point(20, 135), AutoSize = true };
					var cmbStatus = new ComboBox { Location = new Point(20, 160), Width = 180, DataSource = Enum.GetValues(typeof(ReservationStatus)), DropDownStyle = ComboBoxStyle.DropDownList };

					var btnOk = new Button { Text = "Spremi", Location = new Point(20, 200), Width = 100, Height = 30, DialogResult = DialogResult.OK };

					modal.Controls.Add(lblOd);
					modal.Controls.Add(dtpOd);
					modal.Controls.Add(lblDo);
					modal.Controls.Add(dtpDo);
					modal.Controls.Add(lblStatus);
					modal.Controls.Add(cmbStatus);
					modal.Controls.Add(btnOk);
					modal.AcceptButton = btnOk;

					if (modal.ShowDialog() == DialogResult.OK)
					{
						var start = dtpOd.Value.Date;
						var end = dtpDo.Value.Date;
						if (start > end)
						{ MessageBox.Show("Datum DO mora biti >= OD!"); return; }

						var status = (ReservationStatus)cmbStatus.SelectedItem!;

						// Disable dugme za vrijeme dodavanja
						btnAddDates.Enabled = btnAddDates.Enabled = false;

						for (var d = start; d <= end; d = d.AddDays(1))
						{
							var r = new Reservation { Date = d, Status = status };
							await AppState.Api.PostAsync<Reservation>($"/api/admin/facilities/{f.Id}/reservations", r);
						}

						btnAddDates.Enabled = btnAddDates.Enabled = true;

						// Osvježi
						var reservations = await AppState.Api.GetAsync<List<Reservation>>($"/api/admin/facilities/{f.Id}/reservations");
						gridRes.DataSource = new System.ComponentModel.BindingList<Reservation>(reservations?.OrderBy(x => x.Date).ToList() ?? new List<Reservation>());
					}
				}
			};

			tab.Controls.Add(split);
			loadButtons.Add(btnLoad);
			tab.Tag = btnLoad;
			return tab;
		}

		private TabPage CreateGalleryTab()
		{
			var tab = new TabPage("Galerije & Slike");
			tab.BackColor = Color.White;

			var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };

			// Automatski prati 1/3 (Top) i 2/3 (Bottom) kadase control resizea
			split.Resize += (s, e) => { split.SplitterDistance = split.Height / 3; };

			// --- TOP: Galerije ---
			var gTop = new GroupBox { Text = "1. Galerije", Dock = DockStyle.Fill, Padding = new Padding(10) };
			var flowTop = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
			var btnLoad = new Button { Text = "Osvježi", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGray };
			var btnAddGal = new Button { Text = "Dodaj Galeriju", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGreen };
			var btnDelGal = new Button { Text = "Obriši Galeriju", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.MistyRose };
			flowTop.Controls.Add(btnLoad);
			flowTop.Controls.Add(btnAddGal);
			flowTop.Controls.Add(btnDelGal);

			// Bitno: Isključujemo ReadOnly da dozvolimo inplace edit za Code, Name, Description (Id skrivamo)
			var gridGalleries = new DataGridView { Dock = DockStyle.Fill, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, BackgroundColor = Color.WhiteSmoke };
			HideTechnicalColumns(gridGalleries);
			gTop.Controls.Add(gridGalleries);
			gTop.Controls.Add(flowTop);

			// --- BOTTOM: Slike ---
			var gBot = new GroupBox { Text = "2. Slike u odabranoj galeriji", Dock = DockStyle.Fill, Padding = new Padding(10) };
			var flowBot = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
			var btnUpload = new Button { Text = "Dodaj Sliku", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightGreen, Enabled = false };
			var btnChangeImg = new Button { Text = "Izmijeni Sliku", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.LightBlue, Enabled = false };
			var btnDelImg = new Button { Text = "Obriši Sliku", AutoSize = true, MinimumSize = new Size(130, 40), Margin = new Padding(5), BackColor = Color.MistyRose, Enabled = false };
			flowBot.Controls.Add(btnUpload);
			flowBot.Controls.Add(btnChangeImg);
			flowBot.Controls.Add(btnDelImg);

			var splitBot = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
			splitBot.Resize += (s, e) => { splitBot.SplitterDistance = (int)(splitBot.Width * 0.66); };

			// Dozvoli inplace edit AltText/itd.
			var gridPhotos = new DataGridView { Dock = DockStyle.Fill, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, BackgroundColor = Color.WhiteSmoke };
			HideTechnicalColumns(gridPhotos);
			// I FileName je ReadOnly interno (samo sa ChangeImg)
			gridPhotos.CellBeginEdit += (s, e) => { if (gridPhotos.Columns[e.ColumnIndex].Name == "FileName") e.Cancel = true; };

			var picPreview = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };

			splitBot.Panel1.Controls.Add(gridPhotos);
			splitBot.Panel2.Controls.Add(picPreview);

			gBot.Controls.Add(splitBot);
			gBot.Controls.Add(flowBot);

			split.Panel1.Controls.Add(gTop);
			split.Panel2.Controls.Add(gBot);
			split.Panel1.Padding = new Padding(10);
			split.Panel2.Padding = new Padding(10);

			// Prikaz slike
			gridPhotos.SelectionChanged += (s, e) =>
			{
				if (gridPhotos.SelectedRows.Count > 0 && gridGalleries.SelectedRows.Count > 0)
				{
					var p = gridPhotos.SelectedRows[0].DataBoundItem as Photo;
					var g = gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery;
					if (p != null && g != null)
					{
						var baseUrl = AppState.Api.BaseUrl.TrimEnd('/');
						picPreview.ImageLocation = $"{baseUrl}/api/read/images/{g.Code}/{p.FileName}";
					}
				}
				else
				{
					picPreview.CancelAsync();
					picPreview.ImageLocation = null;
				}
			};

			// -- AKCIJE GALERIJE -- //
			BindingList<PhotoGallery>? currentGalleries = null;

			btnLoad.Click += async (s, e) =>
			{
				var data = await AppState.Api.GetAsync<List<PhotoGallery>>("/api/admin/photogalleries");
				if (data != null)
				{
					currentGalleries = new BindingList<PhotoGallery>(data);
					gridGalleries.DataSource = currentGalleries;
				}
				// Ako nema galerija, resetiramo prikaz slika
				if (gridGalleries.SelectedRows.Count == 0 || gridGalleries.DataSource == null)
				{
					gridPhotos.DataSource = null;
					picPreview.CancelAsync();
					picPreview.ImageLocation = null;
					btnUpload.Enabled = btnChangeImg.Enabled = btnDelImg.Enabled = false;
				}
				else
				{
					// Ako se uspješno napunilo i prva je odmah odabrana (što WinForms obično automatski napravi)
					var gallery = gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery;
					if (gallery != null && gallery.Photos != null)
					{
						gridPhotos.DataSource = new BindingList<Photo>(gallery.Photos);
						btnUpload.Enabled = btnChangeImg.Enabled = btnDelImg.Enabled = true;
					}
				}
			};
			loadButtons.Add(btnLoad);
			tab.Tag = btnLoad;

			gridGalleries.SelectionChanged += (s, e) =>
			{
				// Promjena aktivne galerije treba pokazati slike:
				if (gridGalleries.SelectedRows.Count > 0)
				{
					var gallery = gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery;
					// Prekidamo ako je to "nova" row od strane Grida
					if (gallery == null)
						return;
					gridPhotos.DataSource = new BindingList<Photo>(gallery.Photos);
					btnUpload.Enabled = btnChangeImg.Enabled = btnDelImg.Enabled = true;
				}
				else
				{
					gridPhotos.DataSource = null;
					btnUpload.Enabled = btnChangeImg.Enabled = btnDelImg.Enabled = false;
				}
			};

			btnAddGal.Click += async (s, e) =>
			{
				var newGal = new PhotoGallery { Code = "new-code", Name = "Nova Galerija" };
				var res = await AppState.Api.PostAsync<PhotoGallery>("/api/admin/photogalleries", newGal);
				if (res != null)
				{ btnLoad.PerformClick(); }
			};

			btnDelGal.Click += async (s, e) =>
			{
				if (gridGalleries.SelectedRows.Count == 0)
					return;
				var g = gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery;
				if (g == null) return;
				if (MessageBox.Show("Obriši?", "Info", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					await AppState.Api.DeleteAsync($"/api/admin/photogalleries/{g.Id}");
					btnLoad.PerformClick();
				}
			};

			// INPLACE EDIT GALERIJA
			gridGalleries.CellValueChanged += async (s, e) =>
			{
				if (e.RowIndex >= 0)
				{
					var g = gridGalleries.Rows[e.RowIndex].DataBoundItem as PhotoGallery;
					if (g == null) return;
					await AppState.Api.PutAsync($"/api/admin/photogalleries/{g.Id}", g);
				}
			};

			// -- AKCIJE SLIKE -- //
			btnUpload.Click += async (s, e) =>
			{
				if (gridGalleries.SelectedRows.Count == 0)
					return;
				var gallery = gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery;
				if (gallery == null) return;
				var galleryId = gallery.Id;

				using var ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.svg" };
				if (ofd.ShowDialog() == DialogResult.OK)
				{
					bool success = await AppState.Api.UploadPhotoAsync(galleryId, ofd.FileName, "Nova slika");
					if (success)
					{

					}
				}
			};

			btnChangeImg.Click += async (s, e) =>
			{
				if (gridPhotos.SelectedRows.Count == 0)
					return;
				var p = gridPhotos.SelectedRows[0].DataBoundItem as Photo;
				if (p == null) return;
				using var ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.svg" };
				if (ofd.ShowDialog() == DialogResult.OK)
				{
					bool success = await AppState.Api.UpdatePhotoAsync(p.Id, ofd.FileName, p.AltText);
					if (success)
					{ 
					}
				}
			};

			btnDelImg.Click += async (s, e) =>
			{
				if (gridPhotos.SelectedRows.Count == 0)
					return;
				var p = gridPhotos.SelectedRows[0].DataBoundItem as Photo;
				if (p == null) return;
				if (MessageBox.Show("Obriši sliku?", "Info", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					await AppState.Api.DeleteAsync($"/api/admin/photos/{p.Id}");
					btnLoad.PerformClick();
				}
			};

			// INPLACE EDIT SLIKE
			gridPhotos.CellValueChanged += async (s, e) =>
			{
				if (e.RowIndex >= 0)
				{
					var p = gridPhotos.Rows[e.RowIndex].DataBoundItem as Photo;
					if (p == null) return;
					// Zovemo UpdatePhotoAsync s null putanjom da azurira samo AltText!
					await AppState.Api.UpdatePhotoAsync(p.Id, null, p.AltText);
				}
			};

			tab.Controls.Add(split);
			return tab;
		}

		private TabPage CreateApiKeyTab()
		{
			var tab = new TabPage("API Ključ");
			tab.BackColor = Color.White;

			var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), FlowDirection = FlowDirection.TopDown, AutoSize = true };

			var lblInfo = new Label { Text = "Upišite domene (odvojene zarezom):", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(5, 5, 5, 15) };
			var txtDomains = new TextBox { Width = 600, Text = "localhost, mysite.hr", Margin = new Padding(5) };
			var btnGenKey = new Button { Text = "Generiraj Trajni Read-Only API Token", AutoSize = true, MinimumSize = new Size(300, 45), BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 15, 5, 25) };
			var txtResult = new TextBox { Width = 900, Height = 150, Multiline = true, ReadOnly = true, Font = new Font("Consolas", 10), Margin = new Padding(5) };

			btnGenKey.Click += async (s, e) =>
			{
				var domains = txtDomains.Text.Split(',');
				for (int i = 0; i < domains.Length; i++)
					domains[i] = domains[i].Trim();

				var key = await AppState.Api.GenerateApiKeyAsync(domains);
				txtResult.Text = key ?? "Greška kod spajanja na API!";
			};

			flow.Controls.Add(lblInfo);
			flow.Controls.Add(txtDomains);
			flow.Controls.Add(btnGenKey);
			flow.Controls.Add(txtResult);

			tab.Controls.Add(flow);
			return tab;
		}
	}
}





