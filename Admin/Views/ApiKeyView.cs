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
	// Tab za generiranje read-only API ključa. Nema učitavanja na prikazu (koristi default no-op LoadAsync).
	public class ApiKeyView : TabView
	{
		public ApiKeyView(ApiClient api) : base(api)
		{
			BackColor = Color.White;

			var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), FlowDirection = FlowDirection.TopDown, AutoSize = true };

			var lblInfo = new Label { Text = Loc.T("apikey.domainsLabel"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(5, 5, 5, 15) };
			var txtDomains = new TextBox { Width = 600, Text = "localhost, mysite.hr", Margin = new Padding(5) };
			var btnGenKey = new Button { Text = Loc.T("apikey.generateBtn"), AutoSize = true, MinimumSize = new Size(300, 45), BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 15, 5, 25) };
			var txtResult = new TextBox { Width = 900, Height = 150, Multiline = true, ReadOnly = true, Font = new Font("Consolas", 10), Margin = new Padding(5) };

			btnGenKey.Click += OnClick(async () =>
			{
				var domains = txtDomains.Text.Split(',');
				for (int i = 0; i < domains.Length; i++)
					domains[i] = domains[i].Trim();

				var key = await Api.GenerateApiKeyAsync(domains);
				txtResult.Text = key ?? Loc.T("apikey.connectError");
			});

			flow.Controls.Add(lblInfo);
			flow.Controls.Add(txtDomains);
			flow.Controls.Add(btnGenKey);
			flow.Controls.Add(txtResult);

			Controls.Add(flow);
		}
	}
}
