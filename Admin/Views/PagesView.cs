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
	// Stranice web-a + tekstovi odabrane stranice (master/detail).
	public class PagesView : TabView
	{
		private readonly Func<Task> _load;

		public PagesView(ApiClient api) : base(api)
		{
			BackColor = Color.White;

			var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
			// Stranice (lijevo) dobivaju ~2/3 jer imaju više stupaca (Code + SEO polja + galerija);
			// tekstovi (desno) imaju samo Code i Content pa im je dovoljna 1/3.
			split.Resize += (s, e) => { split.SplitterDistance = split.Width * 2 / 3; };

			// --- LJEVI DIO: Stranice ---
			var gLeft = new GroupBox { Text = Loc.T("pages.group1"), Dock = DockStyle.Fill, Padding = new Padding(10) };
			var flowLeft = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
			var btnLoad = new Button { Text = Loc.T("common.refresh"), AutoSize = true, MinimumSize = new Size(80, 40), Margin = new Padding(2), BackColor = Color.LightGray };
			var btnAddPage = new Button { Text = Loc.T("common.add"), AutoSize = true, MinimumSize = new Size(80, 40), Margin = new Padding(2), BackColor = Color.LightGreen };
			var btnDelPage = new Button { Text = Loc.T("common.delete"), AutoSize = true, MinimumSize = new Size(80, 40), Margin = new Padding(2), BackColor = Color.MistyRose };
			flowLeft.Controls.Add(btnLoad);
			flowLeft.Controls.Add(btnAddPage);
			flowLeft.Controls.Add(btnDelPage);

			// Eksplicitni stupci u traženom redoslijedu: prvo Code, zatim SEO polja, pa izbor galerije na kraju.
			// (AutoGenerateColumns isključen da imamo punu kontrolu nad redoslijedom i širinama.)
			var gridPages = MakeGrid();
			gridPages.AutoGenerateColumns = false;
			gridPages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = Loc.T("pages.colCode"), DataPropertyName = "Code", FillWeight = 70 });
			gridPages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = Loc.T("pages.colSeoTitle"), DataPropertyName = "Title", FillWeight = 120 });
			gridPages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = Loc.T("pages.colSeoDescription"), DataPropertyName = "Description", FillWeight = 200 });
			gridPages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Keywords", HeaderText = Loc.T("pages.colSeoKeywords"), DataPropertyName = "Keywords", FillWeight = 130 });
			gridPages.Columns.Add(new DataGridViewComboBoxColumn { Name = "GalleryCombo", HeaderText = Loc.T("pages.colGallery"), DataPropertyName = "PhotoGalleryId", ValueMember = "Id", DisplayMember = "Code", FillWeight = 90 });
			gLeft.Controls.Add(gridPages);
			gLeft.Controls.Add(flowLeft);

			// --- DESNI DIO: Tekstovi ---
			var gRight = new GroupBox { Text = Loc.T("pages.group2"), Dock = DockStyle.Fill, Padding = new Padding(10) };
			var flowRight = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
			var btnAddText = new Button { Text = Loc.T("pages.addText"), AutoSize = true, MinimumSize = new Size(100, 40), Margin = new Padding(5), BackColor = Color.LightGreen, Enabled = false };
			var btnDelText = new Button { Text = Loc.T("pages.delText"), AutoSize = true, MinimumSize = new Size(100, 40), Margin = new Padding(5), BackColor = Color.MistyRose, Enabled = false };
			flowRight.Controls.Add(btnAddText);
			flowRight.Controls.Add(btnDelText);

			var gridTexts = MakeGrid();
			HideTechnicalColumns(gridTexts);
			LocalizeColumns(gridTexts);
			gRight.Controls.Add(gridTexts);
			gRight.Controls.Add(flowRight);

			split.Panel1.Controls.Add(gLeft);
			split.Panel2.Controls.Add(gRight);

			// LOADER
			async Task ReloadPagesAsync()
			{
				var pages = await Api.GetAsync<List<Page>>("/api/admin/pages");
				var gals = await Api.GetAsync<List<PhotoGallery>>("/api/admin/photogalleries");

				// Combo stupac za galeriju definiran je u konstruktoru; ovdje samo osvježimo njegov izvor podataka.
				((DataGridViewComboBoxColumn)gridPages.Columns["GalleryCombo"]!).DataSource = gals;

				gridPages.DataSource = ToBindingList(pages);
			}

			// Učitavanje tekstova odabrane stranice (ili reset ako stranica nije odabrana).
			// Izdvojeno da se može pozvati nakon dodavanja/brisanja teksta, ne samo iz SelectionChanged.
			async Task ReloadTextsAsync(Page? page)
			{
				if (page == null)
				{
					gridTexts.DataSource = null;
					btnAddText.Enabled = btnDelText.Enabled = false;
					return;
				}
				var texts = await Api.GetAsync<List<PageText>>($"/api/admin/pages/{page.Id}/texts");
				gridTexts.DataSource = ToBindingList(texts);
				btnAddText.Enabled = btnDelText.Enabled = true;
			}

			btnLoad.Click += OnClick(() => ReloadPagesAsync());

			// SELECTION CHANGED
			gridPages.SelectionChanged += async (s, e) =>
			{
				var p = (gridPages.SelectedRows.Count > 0 && gridPages.DataSource != null)
					? gridPages.SelectedRows[0].DataBoundItem as Page
					: null;
				await ReloadTextsAsync(p);
			};

			// IN-PLACE EDIT
			gridPages.CellEndEdit += async (s, e) =>
			{
				var p = gridPages.Rows[e.RowIndex].DataBoundItem as Page;
				if (p == null) return;
				await Api.PutAsync($"/api/admin/pages/{p.Id}", p);
			};

			gridTexts.CellEndEdit += async (s, e) =>
			{
				var t = gridTexts.Rows[e.RowIndex].DataBoundItem as PageText;
				if (t == null) return;
				await Api.PutAsync($"/api/admin/pagetexts/{t.Id}", t);
			};

			// BRISANJE I DODAVANJE STRANICA
			btnDelPage.Click += OnClick(async () =>
			{
				if (gridPages.SelectedRows.Count > 0 && MessageBox.Show(Loc.T("pages.delPageConfirm"), Loc.T("common.confirm"), MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var p = gridPages.SelectedRows[0].DataBoundItem as Page;
					if (p == null) return;
					await Api.DeleteAsync($"/api/admin/pages/{p.Id}");
					await ReloadPagesAsync();
				}
			});

			btnAddPage.Click += OnClick(async () =>
			{
				var code = Dialogs.AskText(Loc.T("pages.newPageTitle"), Loc.T("pages.newPageLabel"), "nova-stranica");
				if (code == null) return;
				await Api.PostAsync<Page>("/api/admin/pages", new Page { Code = code });
				await ReloadPagesAsync();
			});

			// BRISANJE I DODAVANJE TEKSTOVA
			btnDelText.Click += OnClick(async () =>
			{
				if (gridTexts.SelectedRows.Count > 0 && MessageBox.Show(Loc.T("pages.delTextConfirm"), Loc.T("common.confirm"), MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var t = gridTexts.SelectedRows[0].DataBoundItem as PageText;
					if (t == null) return;
					await Api.DeleteAsync($"/api/admin/pagetexts/{t.Id}");

					var p = gridPages.SelectedRows[0].DataBoundItem as Page;
					if (p == null) return;
					await ReloadTextsAsync(p);
				}
			});

			btnAddText.Click += OnClick(async () =>
			{
				var p = gridPages.SelectedRows[0].DataBoundItem as Page;
				if (p == null) return;

				// Dijalog se gradi odmah u skaliranim mjerama (font + sve koordinate × zoom).
				// AutoScaleMode.None da se naše ručno skaliranje ne sudara s automatskim.
				int Z(int v) => UiZoom.Scaled(v);

				using (var modal = new Form
				{
					Text = Loc.T("pages.textDialogTitle"),
					StartPosition = FormStartPosition.CenterParent,
					FormBorderStyle = FormBorderStyle.FixedDialog,
					MaximizeBox = false,
					MinimizeBox = false,
					AutoScaleMode = AutoScaleMode.None,
					Font = UiZoom.ScaledFont(9),
					ClientSize = new Size(Z(470), Z(340))
				})
				{
					var lblCode = new Label { Text = Loc.T("pages.textCodeLabel"), Location = new Point(Z(16), Z(16)), AutoSize = true };
					var txtCode = new TextBox { Location = new Point(Z(16), Z(42)), Width = Z(438), Text = "blok-vazno" };

					var lblContent = new Label { Text = Loc.T("pages.textContentLabel"), Location = new Point(Z(16), Z(82)), AutoSize = true };
					var txtContent = new TextBox { Location = new Point(Z(16), Z(108)), Width = Z(438), Height = Z(158), Multiline = true, ScrollBars = ScrollBars.Vertical };

					var btnOk = new Button { Text = Loc.T("common.save"), Location = new Point(Z(16), Z(286)), Width = Z(130), Height = Z(38), DialogResult = DialogResult.OK };
					var btnCancel = new Button { Text = Loc.T("common.cancel"), Location = new Point(Z(156), Z(286)), Width = Z(130), Height = Z(38), DialogResult = DialogResult.Cancel };

					modal.Controls.Add(lblCode);
					modal.Controls.Add(txtCode);
					modal.Controls.Add(lblContent);
					modal.Controls.Add(txtContent);
					modal.Controls.Add(btnOk);
					modal.Controls.Add(btnCancel);
					modal.AcceptButton = btnOk;
					modal.CancelButton = btnCancel;

					if (modal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtCode.Text))
					{
						var t = new PageText { Code = txtCode.Text, Content = txtContent.Text };
						await Api.PostAsync<PageText>($"/api/admin/pages/{p.Id}/texts", t);

						await ReloadTextsAsync(p);
					}
				}
			});

			Controls.Add(split);

			_load = () => ReloadPagesAsync();
		}

		public override Task LoadAsync() => _load();
	}
}
