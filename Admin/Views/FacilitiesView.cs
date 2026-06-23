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
	// Objekti (Facilities) + zauzetost/rezervacije odabranog objekta (master/detail).
	public class FacilitiesView : TabView
	{
		private readonly Func<Task> _load;

		public FacilitiesView(ApiClient api) : base(api)
		{
			BackColor = Color.White;

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

			var gridFac = MakeGrid();
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

			var gridRes = MakeGrid();
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
			async Task ReloadFacilitiesAsync()
			{
				var facs = await Api.GetAsync<List<Facility>>("/api/admin/facilities");
				var gals = await Api.GetAsync<List<PhotoGallery>>("/api/admin/photogalleries");

				if (!gridFac.Columns.Contains("GalleryCombo"))
				{
					var combo = new DataGridViewComboBoxColumn { Name = "GalleryCombo", HeaderText = "Galerija (Code)", DataPropertyName = "PhotoGalleryId", ValueMember = "Id", DisplayMember = "Code", DataSource = gals };
					gridFac.Columns.Add(combo);
				}
				else
				{
					(gridFac.Columns["GalleryCombo"] as DataGridViewComboBoxColumn)!.DataSource = gals;
				}

				gridFac.DataSource = ToBindingList(facs);
			}

			// Učitavanje rezervacija/zauzetosti odabranog objekta (ili reset ako objekt nije odabran).
			// Izdvojeno da se može pozvati nakon dodavanja/brisanja datuma, ne samo iz SelectionChanged.
			async Task ReloadReservationsAsync(Facility? f)
			{
				if (f == null)
				{
					gridRes.DataSource = null;
					btnAddDates.Enabled = btnDelDate.Enabled = false;
					return;
				}
				var reservations = await Api.GetAsync<List<Reservation>>($"/api/admin/facilities/{f.Id}/reservations");

				if (!gridRes.Columns.Contains("StatusCombo"))
				{
					var comboStatus = new DataGridViewComboBoxColumn { Name = "StatusCombo", HeaderText = "Status", DataPropertyName = "Status", DataSource = Enum.GetValues(typeof(ReservationStatus)) };
					gridRes.Columns.Add(comboStatus);
				}
				gridRes.DataSource = ToBindingList(reservations?.OrderBy(x => x.Date).ToList());
				btnAddDates.Enabled = btnDelDate.Enabled = true;

				// Onemogući edit na samom datumu, korisnik uređuje samo Status ili dodaje/briše datume
				if (gridRes.Columns["Date"] != null)
					gridRes.Columns["Date"]!.ReadOnly = true;
			}

			btnLoad.Click += OnClick(() => ReloadFacilitiesAsync());

			// Učitaj rezervacije prelaskom miša:
			gridFac.SelectionChanged += async (s, e) =>
			{
				var f = (gridFac.SelectedRows.Count > 0 && gridFac.DataSource != null)
					? gridFac.SelectedRows[0].DataBoundItem as Facility
					: null;
				await ReloadReservationsAsync(f);
			};

			// IZMJENE (In-place edit)
			gridFac.CellEndEdit += async (s, e) =>
			{
				var f = gridFac.Rows[e.RowIndex].DataBoundItem as Facility;
				if (f == null) return;
				await Api.PutAsync($"/api/admin/facilities/{f.Id}", f);
			};

			gridRes.CellEndEdit += async (s, e) =>
			{
				var r = gridRes.Rows[e.RowIndex].DataBoundItem as Reservation;
				if (r == null) return;
				await Api.PutAsync($"/api/admin/reservations/{r.Id}", r);
			};

			// BRISANJE I DODAVANJE OBJEKATA
			btnDelFac.Click += OnClick(async () =>
			{
				if (gridFac.SelectedRows.Count > 0 && MessageBox.Show("Obrisati objekt?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var f = gridFac.SelectedRows[0].DataBoundItem as Facility;
					if (f == null) return;
					await Api.DeleteAsync($"/api/admin/facilities/{f.Id}");
					await ReloadFacilitiesAsync();
				}
			});

			btnAddFac.Click += OnClick(async () =>
			{
				var code = Dialogs.AskText("Novi Objekt", "Šifra Objekta (Code):", "novi-objekt");
				if (code == null) return;
				await Api.PostAsync<Facility>("/api/admin/facilities", new Facility { Code = code, Name = "Novi Objekt" });
				await ReloadFacilitiesAsync();
			});

			btnDelDate.Click += OnClick(async () =>
			{
				if (gridRes.SelectedRows.Count > 0 && MessageBox.Show("Obrisati zapis datuma?", "Potvrda", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					var r = gridRes.SelectedRows[0].DataBoundItem as Reservation;
					if (r == null) return;
					await Api.DeleteAsync($"/api/admin/reservations/{r.Id}");

					// Osvježi grid rezervacija
					var f = gridFac.SelectedRows[0].DataBoundItem as Facility;
					if (f == null) return;
					await ReloadReservationsAsync(f);
				}
			});

			btnAddDates.Click += OnClick(async () =>
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
						btnAddDates.Enabled = false;

						for (var d = start; d <= end; d = d.AddDays(1))
						{
							var r = new Reservation { Date = d, Status = status };
							await Api.PostAsync<Reservation>($"/api/admin/facilities/{f.Id}/reservations", r);
						}

						// Osvježi (ReloadReservationsAsync ponovno omogućuje gumbe)
						await ReloadReservationsAsync(f);
					}
				}
			});

			Controls.Add(split);

			_load = () => ReloadFacilitiesAsync();
		}

		public override Task LoadAsync() => _load();
	}
}
