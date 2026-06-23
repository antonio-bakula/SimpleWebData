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
			// Gradimo odmah u skaliranim mjerama (font + sve koordinate × zoom);
			// AutoScaleMode.None da se naše ručno skaliranje ne sudara s automatskim.
			int Z(int v) => UiZoom.Scaled(v);

			this.Text = "Login - SimpleWebData Admin";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.BackColor = Color.WhiteSmoke;
			this.AutoScaleMode = AutoScaleMode.None;
			this.Font = UiZoom.ScaledFont(9);
			// Prozor naraste prema sadržaju: kad se pojavi duga poruka greške,
			// donji rub se pomakne da je u cijelosti vidljiva (umjesto da bude odrezana).
			this.AutoSize = true;
			this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.MinimumSize = new Size(Z(420), Z(360));
			this.Padding = new Padding(0, 0, 0, Z(16));

			var pnlTop = new Panel { Dock = DockStyle.Top, Height = Z(60), BackColor = Color.SteelBlue };
			var lblTitle = new Label { Text = "Sustav za upravljanje", ForeColor = Color.White, Font = UiZoom.ScaledFont(12, FontStyle.Bold), Location = new Point(Z(20), Z(15)), AutoSize = true };
			pnlTop.Controls.Add(lblTitle);

			var lblApi = new Label { Text = "API URL:", Location = new Point(Z(30), Z(90)), AutoSize = true };
			txtApiUrl = new TextBox { Location = new Point(Z(140), Z(90)), Width = Z(230), Text = "http://localhost:5072" };

			var lblUser = new Label { Text = "Username:", Location = new Point(Z(30), Z(130)), AutoSize = true };
			txtUsername = new TextBox { Location = new Point(Z(140), Z(130)), Width = Z(230) };

			var lblPass = new Label { Text = "Password:", Location = new Point(Z(30), Z(170)), AutoSize = true };
			txtPassword = new TextBox { Location = new Point(Z(140), Z(170)), Width = Z(230), UseSystemPasswordChar = true };

#if DEBUG
			txtUsername.Text = "admin";
			txtPassword.Text = "123";
#endif

			btnLogin = new Button { Text = "Prijava", Location = new Point(Z(140), Z(220)), Width = Z(230), Height = Z(40), BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
			btnLogin.FlatAppearance.BorderSize = 0;
			btnLogin.Click += BtnLogin_Click;

			// AutoSize + fiksna širina (Min == Max) => label se prelama u više redaka i raste u visinu;
			// tekst ostaje centriran, a forma (AutoSize) naraste s njim pa poruka nikad ne ispadne van.
			lblError = new Label { ForeColor = Color.Crimson, Location = new Point(Z(30), Z(275)), AutoSize = true, MinimumSize = new Size(Z(340), 0), MaximumSize = new Size(Z(340), 0), TextAlign = ContentAlignment.MiddleCenter };

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