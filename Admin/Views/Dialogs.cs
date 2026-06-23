using System.Drawing;
using System.Windows.Forms;
using SimpleWebDataAdmin.Services;

namespace SimpleWebDataAdmin.Views
{
	// Jednostavni reusable modalni dijalozi.
	public static class Dialogs
	{
		// Modal s jednim text poljem (npr. unos šifre/Code). Vraća upisani tekst,
		// ili null ako je korisnik odustao ili ostavio prazno.
		public static string? AskText(string title, string label, string defaultValue = "")
		{
			// Dijalog se gradi odmah u skaliranim mjerama (font + sve koordinate × zoom).
			int Z(int v) => UiZoom.Scaled(v);

			using var modal = new Form
			{
				Text = title,
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false,
				AutoScaleMode = AutoScaleMode.None,
				Font = UiZoom.ScaledFont(9),
				ClientSize = new Size(Z(380), Z(160))
			};
			var lbl = new Label { Text = label, Location = new Point(Z(16), Z(16)), AutoSize = true };
			var txt = new TextBox { Location = new Point(Z(16), Z(44)), Width = Z(348), Text = defaultValue };
			var btnOk = new Button { Text = "Spremi", Location = new Point(Z(16), Z(104)), Width = Z(110), Height = Z(36), DialogResult = DialogResult.OK };
			var btnCancel = new Button { Text = "Odustani", Location = new Point(Z(136), Z(104)), Width = Z(110), Height = Z(36), DialogResult = DialogResult.Cancel };
			modal.Controls.Add(lbl);
			modal.Controls.Add(txt);
			modal.Controls.Add(btnOk);
			modal.Controls.Add(btnCancel);
			modal.AcceptButton = btnOk;
			modal.CancelButton = btnCancel;

			if (modal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
				return txt.Text;
			return null;
		}
	}
}
