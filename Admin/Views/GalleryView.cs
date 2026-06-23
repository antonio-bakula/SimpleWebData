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
	// Galerije + slike u odabranoj galeriji (master/detail) s pregledom slike.
	public class GalleryView : TabView
	{
		private readonly Func<Task> _load;

		public GalleryView(ApiClient api) : base(api)
		{
			BackColor = Color.White;

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
			var gridGalleries = MakeGrid();
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
			var gridPhotos = MakeGrid();
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
						var baseUrl = Api.BaseUrl.TrimEnd('/');
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

			// Prikaz slika odabrane galerije (ili reset ako galerija nije odabrana).
			// Izdvojeno da se može pozvati i nakon uploada/izmjene/brisanja, ne samo iz SelectionChanged.
			void ShowPhotos(PhotoGallery? gallery)
			{
				if (gallery == null)
				{
					gridPhotos.DataSource = null;
					picPreview.CancelAsync();
					picPreview.ImageLocation = null;
					btnUpload.Enabled = btnChangeImg.Enabled = btnDelImg.Enabled = false;
					return;
				}
				gridPhotos.DataSource = ToBindingList(gallery.Photos);
				btnUpload.Enabled = btnChangeImg.Enabled = btnDelImg.Enabled = true;
			}

			int? SelectedGalleryId() =>
				(gridGalleries.SelectedRows.Count > 0
					? gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery
					: null)?.Id;

			void SelectGalleryById(int id)
			{
				foreach (DataGridViewRow row in gridGalleries.Rows)
				{
					if (row.DataBoundItem is PhotoGallery g && g.Id == id)
					{
						gridGalleries.ClearSelection();
						row.Selected = true;
						return;
					}
				}
			}

			// Centralno učitavanje galerija. Zamjenjuje btnLoad.PerformClick() kao mehanizam osvježavanja.
			// selectGalleryId: ako je zadan, nakon reloada ostajemo na toj galeriji (npr. nakon uploada slike).
			async Task ReloadGalleriesAsync(int? selectGalleryId = null)
			{
				var data = await Api.GetAsync<List<PhotoGallery>>("/api/admin/photogalleries");
				currentGalleries = ToBindingList(data);
				gridGalleries.DataSource = currentGalleries;

				if (currentGalleries.Count == 0)
				{
					ShowPhotos(null);
					return;
				}

				if (selectGalleryId != null)
					SelectGalleryById(selectGalleryId.Value);

				// Eksplicitno osvježi prikaz slika za trenutno odabranu galeriju.
				// Ne oslanjamo se samo na SelectionChanged zbog WinForms timinga kod (re)bindanja.
				var selected = gridGalleries.SelectedRows.Count > 0
					? gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery
					: null;
				ShowPhotos(selected ?? currentGalleries[0]);
			}

			btnLoad.Click += OnClick(() => ReloadGalleriesAsync());

			gridGalleries.SelectionChanged += (s, e) =>
			{
				// Promjena aktivne galerije treba pokazati slike:
				if (gridGalleries.SelectedRows.Count > 0)
				{
					var gallery = gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery;
					// Prekidamo ako je to tranzijentni null tijekom (re)bindanja grida
					if (gallery == null)
						return;
					ShowPhotos(gallery);
				}
				else
				{
					ShowPhotos(null);
				}
			};

			btnAddGal.Click += OnClick(async () =>
			{
				var newGal = new PhotoGallery { Code = "new-code", Name = "Nova Galerija" };
				var res = await Api.PostAsync<PhotoGallery>("/api/admin/photogalleries", newGal);
				if (res != null)
					await ReloadGalleriesAsync(res.Id);
				else
					MessageBox.Show("Dodavanje galerije nije uspjelo.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
			});

			btnDelGal.Click += OnClick(async () =>
			{
				if (gridGalleries.SelectedRows.Count == 0)
					return;
				var g = gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery;
				if (g == null) return;
				if (MessageBox.Show("Obriši?", "Info", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					await Api.DeleteAsync($"/api/admin/photogalleries/{g.Id}");
					await ReloadGalleriesAsync();
				}
			});

			// INPLACE EDIT GALERIJA
			gridGalleries.CellValueChanged += async (s, e) =>
			{
				if (e.RowIndex >= 0)
				{
					var g = gridGalleries.Rows[e.RowIndex].DataBoundItem as PhotoGallery;
					if (g == null) return;
					await Api.PutAsync($"/api/admin/photogalleries/{g.Id}", g);
				}
			};

			// -- AKCIJE SLIKE -- //
			btnUpload.Click += OnClick(async () =>
			{
				if (gridGalleries.SelectedRows.Count == 0)
					return;
				var gallery = gridGalleries.SelectedRows[0].DataBoundItem as PhotoGallery;
				if (gallery == null) return;
				var galleryId = gallery.Id;

				using var ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.svg" };
				if (ofd.ShowDialog() == DialogResult.OK)
				{
					bool success = await Api.UploadPhotoAsync(galleryId, ofd.FileName, "Nova slika");
					if (success)
						await ReloadGalleriesAsync(galleryId);   // #6: osvježi grid i ostani na istoj galeriji
					else
						MessageBox.Show("Upload slike nije uspio.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			});

			btnChangeImg.Click += OnClick(async () =>
			{
				if (gridPhotos.SelectedRows.Count == 0)
					return;
				var p = gridPhotos.SelectedRows[0].DataBoundItem as Photo;
				if (p == null) return;
				var galleryId = SelectedGalleryId();
				using var ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.svg" };
				if (ofd.ShowDialog() == DialogResult.OK)
				{
					bool success = await Api.UpdatePhotoAsync(p.Id, ofd.FileName, p.AltText);
					if (success)
						await ReloadGalleriesAsync(galleryId);   // osvježi prikaz nove slike
					else
						MessageBox.Show("Izmjena slike nije uspjela.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			});

			btnDelImg.Click += OnClick(async () =>
			{
				if (gridPhotos.SelectedRows.Count == 0)
					return;
				var p = gridPhotos.SelectedRows[0].DataBoundItem as Photo;
				if (p == null) return;
				var galleryId = SelectedGalleryId();
				if (MessageBox.Show("Obriši sliku?", "Info", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					await Api.DeleteAsync($"/api/admin/photos/{p.Id}");
					await ReloadGalleriesAsync(galleryId);
				}
			});

			// INPLACE EDIT SLIKE
			gridPhotos.CellValueChanged += async (s, e) =>
			{
				if (e.RowIndex >= 0)
				{
					var p = gridPhotos.Rows[e.RowIndex].DataBoundItem as Photo;
					if (p == null) return;
					// Zovemo UpdatePhotoAsync s null putanjom da azurira samo AltText!
					await Api.UpdatePhotoAsync(p.Id, null, p.AltText);
				}
			};

			Controls.Add(split);

			_load = () => ReloadGalleriesAsync();
		}

		public override Task LoadAsync() => _load();
	}
}
