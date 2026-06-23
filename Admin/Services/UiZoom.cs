using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Forms;

namespace SimpleWebDataAdmin.Services
{
    // Centralna "veličina prikaza" (zoom) za cijelu aplikaciju.
    //
    // Dvije strategije skaliranja, jer se rasporedi razlikuju:
    //  - ApplyFontZoom: skalira SAMO font (glavni prozor koristi docking/flow raspored
    //    pa se kontrole same preslože kad font naraste). Re-primjenjiv je više puta jer
    //    pamti originalni font svake kontrole (apsolutno skaliranje, bez gomilanja greške).
    //  - Scaled()/ScaledFont(): apsolutno pozicionirane forme (login i modalni dijalozi)
    //    grade se odmah u skaliranim mjerama. Pouzdanije od Form.Scale, koji na u kodu
    //    građenim formama ne skalira font pouzdano.
    public static class UiZoom
    {
        // Originalni (100%) font po kontroli; slabe reference da uništene kontrole ne cure.
        private static readonly ConditionalWeakTable<Control, Font> _baseFonts = new();

        public static double Factor { get; private set; } = 1.0;

        public static void SetFactor(double factor) => Factor = ClampFactor(factor);

        // Skalira font cijelog stabla kontrola na zadani faktor (1.0 = 100%).
        // DVA prolaza su bitna: prvo zapamtimo SVE originalne fontove BEZ ikakve izmjene, pa tek
        // onda skaliramo. Inače bi kontrole koje nasljeđuju font roditelja zapamtile roditeljev
        // VEĆ uvećani font kao "original" i skaliranje bi se gomilalo (1.5 × 1.5 × ...).
        public static void ApplyFontZoom(Control root, double factor)
        {
            Factor = ClampFactor(factor);

            root.SuspendLayout();
            try
            {
                CaptureBaseFonts(root);       // prolaz 1: samo čitanje i pamćenje originala
                ApplyBaseFonts(root, Factor); // prolaz 2: font = original * faktor
            }
            finally
            {
                root.ResumeLayout(true);
            }
        }

        // Prolaz 1: zapamti originalni font svake kontrole (ne mijenja ništa).
        private static void CaptureBaseFonts(Control c)
        {
            if (!_baseFonts.TryGetValue(c, out _))
                _baseFonts.Add(c, c.Font);
            foreach (Control child in c.Controls)
                CaptureBaseFonts(child);
        }

        // Prolaz 2: postavi font svake kontrole na original * faktor.
        private static void ApplyBaseFonts(Control c, double factor)
        {
            if (_baseFonts.TryGetValue(c, out var baseFont))
                c.Font = new Font(baseFont.FontFamily, (float)(baseFont.Size * factor), baseFont.Style);

            // Gridovima pustimo da redci/zaglavlja narastu uz font, inače bi veći tekst bio odrezan.
            if (c is DataGridView dgv)
            {
                dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            }

            foreach (Control child in c.Controls)
                ApplyBaseFonts(child, factor);
        }

        // Pomoćnik za "ručno" građene dijaloge: vrati piksel-vrijednost (koordinata/širina/visina)
        // pomnoženu trenutnim faktorom zooma. Tako dijalog gradimo odmah u skaliranim mjerama.
        public static int Scaled(int value) => (int)Math.Round(value * Factor);

        // Skalirani font za dijaloge (font cijelog dijaloga; kontrole ga naslijede).
        public static Font ScaledFont(float basePt, FontStyle style = FontStyle.Regular)
            => new Font("Segoe UI", (float)(basePt * Factor), style);

        // Učita zapamćeni zoom iz postavki (dijeli settings.json s podacima o prozoru).
        public static void Load(string path = "settings.json")
        {
            try
            {
                if (!File.Exists(path))
                    return;
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("Zoom", out var z) &&
                    z.TryGetDouble(out var v))
                {
                    Factor = ClampFactor(v);
                }
            }
            catch { /* Utišano - ako ne uspije, ostaje 100% */ }
        }

        private static double ClampFactor(double f) => f < 1.0 ? 1.0 : (f > 2.0 ? 2.0 : f);
    }
}
