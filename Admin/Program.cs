using System;
using System.Windows.Forms;

namespace SimpleWebDataAdmin
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); ApplicationConfiguration.Initialize();

            using var loginForm = new Forms.LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new Forms.MainForm(AppState.Api));
            }
        }
    }
}
