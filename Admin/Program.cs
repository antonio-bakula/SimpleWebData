using System;
using System.Windows.Forms;
using SimpleWebDataAdmin.Services;

namespace SimpleWebDataAdmin
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); ApplicationConfiguration.Initialize();

            // Globalna sigurnosna mreža: iznimke iz async void event handlera (Click, CellEndEdit, …)
            // inače bi srušile aplikaciju. Ovdje ih hvatamo i prikažemo umjesto pada.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
                MessageBox.Show($"Došlo je do greške: {e.Exception.Message}", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Učitaj zapamćene postavke (veličina prikaza + jezik) da se primijene već na login.
            UiZoom.Load();
            Loc.Load();

            using var loginForm = new Forms.LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new Forms.MainForm(AppState.Api));
            }
        }
    }
}
