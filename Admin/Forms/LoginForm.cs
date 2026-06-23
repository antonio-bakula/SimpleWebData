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
		private Label lblTitle = null!;
		private Label lblApi = null!;
		private Label lblUser = null!;
		private Label lblPass = null!;
		private ComboBox cmbLang = null!;

		public LoginForm()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			// Gradimo odmah u skaliranim mjerama (font + sve koordinate × zoom);
			// AutoScaleMode.None da se naše ručno skaliranje ne sudara s automatskim.
			int Z(int v) => UiZoom.Scaled(v);

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
			this.MinimumSize = new Size(Z(480), Z(410));
			this.Padding = new Padding(0, 0, 0, Z(16));

			var pnlTop = new Panel { Dock = DockStyle.Top, Height = Z(60), BackColor = Color.SteelBlue };
			lblTitle = new Label { ForeColor = Color.White, Font = UiZoom.ScaledFont(12, FontStyle.Bold), Location = new Point(Z(20), Z(15)), AutoSize = true };
			pnlTop.Controls.Add(lblTitle);

			// Izbor jezika (oznaka je namjerno dvojezična da je uvijek jasna).
			var lblLang = new Label { Text = "Language:", Location = new Point(Z(30), Z(82)), AutoSize = true };
			cmbLang = new ComboBox { Location = new Point(Z(185), Z(78)), Width = Z(235), DropDownStyle = ComboBoxStyle.DropDownList };
			cmbLang.Items.AddRange(new object[] { "Hrvatski", "English" });
			cmbLang.SelectedIndex = Loc.Language == "en" ? 1 : 0;
			cmbLang.SelectedIndexChanged += (s, e) =>
			{
				Loc.Language = cmbLang.SelectedIndex == 1 ? "en" : "hr";
				ApplyTexts();
			};

			lblApi = new Label { Location = new Point(Z(30), Z(122)), AutoSize = true };
			txtApiUrl = new TextBox { Location = new Point(Z(185), Z(122)), Width = Z(235), Text = "http://localhost:5072" };

			lblUser = new Label { Location = new Point(Z(30), Z(162)), AutoSize = true };
			txtUsername = new TextBox { Location = new Point(Z(185), Z(162)), Width = Z(235) };

			lblPass = new Label { Location = new Point(Z(30), Z(202)), AutoSize = true };
			txtPassword = new TextBox { Location = new Point(Z(185), Z(202)), Width = Z(235), UseSystemPasswordChar = true };

#if DEBUG
			txtUsername.Text = "admin";
			txtPassword.Text = "123";
#endif

			btnLogin = new Button { Location = new Point(Z(185), Z(252)), Width = Z(235), Height = Z(40), BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
			btnLogin.FlatAppearance.BorderSize = 0;
			btnLogin.Click += BtnLogin_Click;

			// AutoSize + fiksna širina (Min == Max) => label se prelama u više redaka i raste u visinu;
			// tekst ostaje centriran, a forma (AutoSize) naraste s njim pa poruka nikad ne ispadne van.
			lblError = new Label { ForeColor = Color.Crimson, Location = new Point(Z(30), Z(307)), AutoSize = true, MinimumSize = new Size(Z(340), 0), MaximumSize = new Size(Z(340), 0), TextAlign = ContentAlignment.MiddleCenter };

			this.Controls.Add(pnlTop);
			this.Controls.Add(lblLang);
			this.Controls.Add(cmbLang);
			this.Controls.Add(lblApi);
			this.Controls.Add(txtApiUrl);
			this.Controls.Add(lblUser);
			this.Controls.Add(txtUsername);
			this.Controls.Add(lblPass);
			this.Controls.Add(txtPassword);
			this.Controls.Add(btnLogin);
			this.Controls.Add(lblError);

			this.AcceptButton = btnLogin;

			ApplyTexts();
		}

		// Postavi sve tekstove prema trenutnom jeziku (poziva se i kod promjene jezika na loginu).
		private void ApplyTexts()
		{
			this.Text = Loc.T("login.windowTitle");
			lblTitle.Text = Loc.T("login.title");
			lblApi.Text = Loc.T("login.apiUrl");
			lblUser.Text = Loc.T("login.username");
			lblPass.Text = Loc.T("login.password");
			btnLogin.Text = Loc.T("login.button");
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
					lblError.Text = Loc.T("login.errLogin");
					btnLogin.Enabled = true;
				}
			}
			catch (Exception ex)
			{
				lblError.Text = Loc.T("login.errConnect") + ex.Message;
				btnLogin.Enabled = true;
			}
		}
	}
}