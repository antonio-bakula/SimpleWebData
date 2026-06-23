using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using SimpleWebDataAdmin.Services;
using SimpleWebDataAdmin.Views;
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
		// Lazy lifecycle: svaki tab nosi svoj loader u TabPage.Tag (Func<Task>) i učita se
		// prvi put kad postane vidljiv. activatedTabs pamti koji su tabovi već učitani.
		private readonly HashSet<TabPage> activatedTabs = new();
		private readonly ApiClient _api;

		// Traka s aktivnim web site-om: lblActiveSite (obični admin) ili cmbSite (super admin bira site).
		private Label lblActiveSite = null!;
		private ComboBox? cmbSite;

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
			// Prvo postavimo web site kontekst (selektor za super admina / prikaz za običnog), pa onda tab.
			try
			{
				await InitWebSiteContextAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Greška kod učitavanja web site konteksta: {ex.Message}", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

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
			_api.SessionExpired += OnSessionExpired;

			bool isSuperAdmin = _api.CurrentUser.IsSuperUser;

			this.Text = isSuperAdmin
				? "Super Admin - SimpleWebData"
				: $"Dobro došao {_api.CurrentUser.Username}";

			BuildSiteHeader(isSuperAdmin);

			// TENANT MODULI
			AddTab("Stranice web-a", new PagesView(_api));
			AddTab("Galerije & Slike", new GalleryView(_api));
			AddTab("Objekti (Facilities)", new FacilitiesView(_api));

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

		// Gradi traku na vrhu koja pokazuje (i za super admina nudi izbor) aktivni web site.
		private void BuildSiteHeader(bool isSuperAdmin)
		{
			var pnlHeader = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				WrapContents = false,
				BackColor = Color.FromArgb(230, 238, 246),
				Padding = new Padding(12, 8, 12, 8)
			};

			var lblPrefix = new Label
			{
				Text = "Aktivni web site:",
				AutoSize = true,
				Font = new Font("Segoe UI", 10F, FontStyle.Bold),
				Margin = new Padding(0, 6, 10, 0)
			};
			pnlHeader.Controls.Add(lblPrefix);

			if (isSuperAdmin)
			{
				// Super admin bira na kojem web site-u radi (popunjava se u InitWebSiteContextAsync).
				cmbSite = new ComboBox
				{
					DropDownStyle = ComboBoxStyle.DropDownList,
					Width = 320,
					Font = new Font("Segoe UI", 10F),
					Margin = new Padding(0, 3, 0, 0)
				};
				pnlHeader.Controls.Add(cmbSite);
			}
			else
			{
				// Obični admin samo vidi svoj site.
				lblActiveSite = new Label
				{
					AutoSize = true,
					Font = new Font("Segoe UI", 10F),
					Margin = new Padding(0, 6, 0, 0)
				};
				pnlHeader.Controls.Add(lblActiveSite);
			}

			// Dodaje se nakon TabControl-a (Dock.Fill) pa zbog redoslijeda dockanja traka ide na vrh, a tabovi pune ostatak.
			this.Controls.Add(pnlHeader);
		}

		// Inicijalno učita web site kontekst: super adminu napuni selektor i postavi aktivni site,
		// običnom adminu prikaže njegov site.
		private async Task InitWebSiteContextAsync()
		{
			if (cmbSite != null)
			{
				var sites = await _api.GetAsync<List<WebSite>>("/api/superadmin/websites") ?? new List<WebSite>();

				cmbSite.DisplayMember = nameof(WebSite.Name);
				cmbSite.ValueMember = nameof(WebSite.Id);
				cmbSite.DataSource = sites;

				// Default: site na koji je super admin vezan u svojim podacima (iz tokena), inače prvi dostupni.
				var defaultSite = sites.FirstOrDefault(s => s.Id == _api.CurrentUser.WebSiteId) ?? sites.FirstOrDefault();
				if (defaultSite != null)
				{
					cmbSite.SelectedValue = defaultSite.Id;
					_api.SetActiveWebSite(defaultSite.Id);
				}

				// Handler vežemo tek sad da se ne okida tijekom inicijalnog popunjavanja.
				cmbSite.SelectedIndexChanged += async (s, e) =>
				{
					try { await OnSiteChangedAsync(); }
					catch (Exception ex) { MessageBox.Show($"Greška kod promjene web site-a: {ex.Message}", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error); }
				};
			}
			else
			{
				var site = await _api.GetAsync<WebSite>("/api/admin/current-website");
				lblActiveSite.Text = site != null ? $"{site.Name} ({site.Code})" : "(nepoznato)";
			}
		}

		// Super admin promijenio aktivni site -> preusmjeri pozive i ponovno učitaj trenutni tab.
		private async Task OnSiteChangedAsync()
		{
			if (cmbSite?.SelectedValue is not int siteId)
				return;

			_api.SetActiveWebSite(siteId);

			// Tenant tabovi moraju prikazati podatke novog site-a: poništavamo keš učitanih tabova
			// i ponovno učitavamo trenutno vidljivi (ostali se učitaju lijeno pri sljedećem prikazu).
			activatedTabs.Clear();
			await ActivateAsync(mainTabControl.SelectedTab);
		}

		private bool _sessionExpiredShown;

		// Refresh tokena nije uspio (refresh token istekao/nevažeći) -> obavijesti korisnika i zatvori
		// aplikaciju da se ponovno prijavi. Marshaliramo na UI nit jer event može doći iz pozadinskog konteksta.
		private void OnSessionExpired()
		{
			if (InvokeRequired)
			{
				BeginInvoke(new Action(OnSessionExpired));
				return;
			}

			if (_sessionExpiredShown)
				return;
			_sessionExpiredShown = true;

			MessageBox.Show("Vaša sesija je istekla. Molimo prijavite se ponovno.",
				"Sesija istekla", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			this.Close();
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
