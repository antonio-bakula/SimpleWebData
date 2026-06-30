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
		public double Zoom { get; set; } = 1.0;
		public string Language { get; set; } = "hr";
		public string ApiUrl { get; set; } = "";
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

		// Zoom (veličina prikaza) - logika je u UiZoom helperu.
		private ComboBox? cmbZoom;

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

						// Vrati zapamćenu veličinu prikaza (zoom).
						if (s.Zoom >= 1.0 && s.Zoom <= 2.0)
							SetZoomCombo(s.Zoom);
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
				MessageBox.Show(Loc.T("main.errLoadSiteContext", ex.Message), Loc.T("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
				MessageBox.Show(Loc.T("main.errLoadTab", ex.Message), Loc.T("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			try
			{
				// ApiUrl prepisujemo natrag (na temelju aktivne konekcije) jer inače bismo ga,
				// pošto ovaj objekt ide preko cijelog settings.json, izbrisali (zapisao ga je login).
				var s = new WindowSettings { WindowState = this.WindowState, Zoom = UiZoom.Factor, Language = Loc.Language, ApiUrl = _api.BaseUrl.TrimEnd('/') };
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
				? Loc.T("main.titleSuper")
				: Loc.T("main.titleWelcome", _api.CurrentUser.Username);

			BuildSiteHeader(isSuperAdmin);

			// TENANT MODULI
			AddTab(Loc.T("tab.pages"), new PagesView(_api));
			AddTab(Loc.T("tab.galleries"), new GalleryView(_api));
			AddTab(Loc.T("tab.facilities"), new FacilitiesView(_api));

			// SUPER ADMIN MODULI
			if (isSuperAdmin)
			{
				AddTab(Loc.T("tab.apiKey"), new ApiKeyView(_api));
				AddTab(Loc.T("tab.saWebSites"), new WebSitesView(_api));
				AddTab(Loc.T("tab.saUsers"), new UsersView(_api));
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
				Text = Loc.T("main.activeSite"),
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

			// Zoom kontrola (veličina prikaza) - vidljiva svima.
			var lblZoom = new Label
			{
				Text = Loc.T("main.displaySize"),
				AutoSize = true,
				Font = new Font("Segoe UI", 10F, FontStyle.Bold),
				Margin = new Padding(40, 6, 8, 0)
			};
			cmbZoom = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 126, // +40% da postotak stane i kad se font poveća (npr. 150%)
				Font = new Font("Segoe UI", 10F),
				Margin = new Padding(0, 3, 0, 0)
			};
			cmbZoom.Items.AddRange(new object[] { "100%", "125%", "150%", "175%", "200%" });
			cmbZoom.SelectedIndex = 0;
			cmbZoom.SelectedIndexChanged += (s, e) => OnZoomChanged();
			pnlHeader.Controls.Add(lblZoom);
			pnlHeader.Controls.Add(cmbZoom);

			// Dodaje se nakon TabControl-a (Dock.Fill) pa zbog redoslijeda dockanja traka ide na vrh, a tabovi pune ostatak.
			this.Controls.Add(pnlHeader);
		}

		// Glavni prozor skalira samo font (docking/flow raspored se sam preslože).
		private void OnZoomChanged()
		{
			if (cmbZoom?.SelectedItem is string txt && int.TryParse(txt.TrimEnd('%'), out int pct))
				UiZoom.ApplyFontZoom(this, pct / 100.0);
		}

		// Postavi combo na spremljeni zoom (okida OnZoomChanged preko handlera); ako nije preset, primijeni izravno.
		private void SetZoomCombo(double zoom)
		{
			if (cmbZoom == null)
				return;
			var target = $"{(int)Math.Round(zoom * 100)}%";
			int idx = cmbZoom.Items.IndexOf(target);
			if (idx >= 0)
				cmbZoom.SelectedIndex = idx;
			else
				UiZoom.ApplyFontZoom(this, zoom);
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
					catch (Exception ex) { MessageBox.Show(Loc.T("main.errChangeSite", ex.Message), Loc.T("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
				};
			}
			else
			{
				var site = await _api.GetAsync<WebSite>("/api/admin/current-website");
				lblActiveSite.Text = site != null ? $"{site.Name} ({site.Code})" : Loc.T("main.unknown");
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

			MessageBox.Show(Loc.T("main.sessionExpiredMsg"),
				Loc.T("main.sessionExpiredTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
