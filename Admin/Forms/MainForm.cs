using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using SimpleWebDataAdmin.Services;
using SimpleWebDataAdmin.Views;

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
		// Lazy lifecycle: svaki tab nosi svoj loader u TabPage.Tag (Func<Task>) i učita se
		// prvi put kad postane vidljiv. activatedTabs pamti koji su tabovi već učitani.
		private readonly HashSet<TabPage> activatedTabs = new();
		private readonly ApiClient _api;

		public MainForm(ApiClient api)
		{
			_api = api;
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

		protected override async void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// Učitavamo samo trenutno odabrani tab; ostali se učitavaju lijeno kad se prvi put otvore.
			await ActivateAsync(mainTabControl.SelectedTab);
		}

		// Lijeno aktivira (učitava) tab i to samo prvi put. TabPage.Tag sadrži loader (Func<Task>).
		// Zamjena za stari mehanizam btnLoad.PerformClick() u OnShown/SelectedIndexChanged.
		private async Task ActivateAsync(TabPage? tab)
		{
			if (tab == null || activatedTabs.Contains(tab))
				return;
			if (tab.Tag is not Func<Task> loader)
				return;

			activatedTabs.Add(tab);
			try
			{
				await loader();
			}
			catch (Exception ex)
			{
				activatedTabs.Remove(tab); // dopusti ponovni pokušaj kod sljedećeg prikaza taba
				MessageBox.Show($"Greška kod učitavanja taba: {ex.Message}", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
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
			bool isSuperAdmin = _api.CurrentUser.IsSuperUser;

			this.Text = isSuperAdmin
				? "Super Admin - SimpleWebData"
				: $"Dobro došao {_api.CurrentUser.Username}";

			// TENANT MODULI
			AddTab("Galerije & Slike", new GalleryView(_api));
			AddTab("Objekti (Facilities)", new FacilitiesView(_api));
			AddTab("Stranice web-a", new PagesView(_api));

			// SUPER ADMIN MODULI
			if (isSuperAdmin)
			{
				AddTab("API Ključ", new ApiKeyView(_api));
				AddTab("[SA] Web Sites", new WebSitesView(_api));
				AddTab("[SA] Korisnici", new UsersView(_api));
			}

			// Lijeno učitavanje pri prvom prelasku na tab. Kasniji prelasci ne re-fetchaju
			// (za osvježavanje služi gumb "Osvježi" u svakom tabu).
			mainTabControl.SelectedIndexChanged += async (s, e) =>
				await ActivateAsync(mainTabControl.SelectedTab);
		}

		// Umota view u TabPage i registrira njegov LoadAsync kao lazy loader (TabPage.Tag).
		private void AddTab(string title, TabView view)
		{
			view.Dock = DockStyle.Fill;
			var tab = new TabPage(title) { BackColor = Color.White };
			tab.Controls.Add(view);
			tab.Tag = (Func<Task>)view.LoadAsync;
			mainTabControl.TabPages.Add(tab);
		}
	}
}
