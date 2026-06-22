using System;
using System.Drawing;
using System.Windows.Forms;
using SimpleWebDataAdmin.Services;

namespace SimpleWebDataAdmin.Forms
{
    public class LoginForm : Form
    {
        private TextBox txtApiUrl = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private Label lblError = null!;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Login - SimpleWebData Admin";
            this.Size = new Size(420, 360);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.WhiteSmoke;
            this.AutoScaleMode = AutoScaleMode.Dpi;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.SteelBlue };
            var lblTitle = new Label { Text = "Sustav za upravljanje", ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
            pnlTop.Controls.Add(lblTitle);

            var lblApi = new Label { Text = "API URL:", Location = new Point(30, 90), AutoSize = true, Font = new Font("Segoe UI", 9) };
            txtApiUrl = new TextBox { Location = new Point(140, 90), Width = 230, Text = "http://localhost:5072" };

            var lblUser = new Label { Text = "Username:", Location = new Point(30, 130), AutoSize = true, Font = new Font("Segoe UI", 9) };
            txtUsername = new TextBox { Location = new Point(140, 130), Width = 230, Text = "admin" }; 

            var lblPass = new Label { Text = "Password:", Location = new Point(30, 170), AutoSize = true, Font = new Font("Segoe UI", 9) };
            txtPassword = new TextBox { Location = new Point(140, 170), Width = 230, UseSystemPasswordChar = true, Text = "123" };

            btnLogin = new Button { Text = "Prijava", Location = new Point(140, 220), Width = 230, Height = 40, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            lblError = new Label { ForeColor = Color.Crimson, Location = new Point(30, 275), Width = 340, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };

            this.Controls.Add(pnlTop);
            this.Controls.Add(lblApi);
            this.Controls.Add(txtApiUrl);
            this.Controls.Add(lblUser);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
            this.Controls.Add(lblError);
            
            this.AcceptButton = btnLogin;
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            try
            {
                btnLogin.Enabled = false;

                // Instanciramo novi ApiClient na osnovu upisanog URL-a kojeg je unio korisnik
                AppState.Api = new ApiClient(txtApiUrl.Text.Trim());

                bool success = await AppState.Api.LoginAsync(txtUsername.Text, txtPassword.Text);

                if (success)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblError.Text = "Greška kod prijave. Pokušajte ponovno.";
                    btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Nije moguće spojiti API: " + ex.Message;
                btnLogin.Enabled = true;
            }
        }
    }
}