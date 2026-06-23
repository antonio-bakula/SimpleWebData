using System.Drawing;
using System.Windows.Forms;

namespace SimpleWebDataAdmin.Views
{
	// Jednostavni reusable modalni dijalozi.
	public static class Dialogs
	{
		// Modal s jednim text poljem (npr. unos šifre/Code). Vraća upisani tekst,
		// ili null ako je korisnik odustao ili ostavio prazno.
		public static string? AskText(string title, string label, string defaultValue = "")
		{
			using var modal = new Form
			{
				ClientSize = new Size(320, 150),
				Text = title,
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false
			};
			var lbl = new Label { Text = label, Location = new Point(20, 15), AutoSize = true };
			var txt = new TextBox { Location = new Point(20, 45), Width = 280, Text = defaultValue };
			var btnOk = new Button { Text = "Spremi", Location = new Point(20, 90), Width = 100, Height = 30, DialogResult = DialogResult.OK };
			modal.Controls.Add(lbl);
			modal.Controls.Add(txt);
			modal.Controls.Add(btnOk);
			modal.AcceptButton = btnOk;

			if (modal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
				return txt.Text;
			return null;
		}
	}
}
