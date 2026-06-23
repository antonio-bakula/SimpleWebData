using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SimpleWebDataAdmin.Services
{
    // Jednostavna lokalizacija UI-ja admin aplikacije (hrvatski / engleski).
    // Prevodi se SAMO UI (oznake, gumbi, zaglavlja kolona, poruke) - ne i podaci.
    // Jezik se bira na loginu i pamti u settings.json (kao i zoom).
    public static class Loc
    {
        // "hr" ili "en"
        public static string Language { get; set; } = "hr";

        public static string T(string key)
        {
            var dict = Language == "en" ? _en : _hr;
            if (dict.TryGetValue(key, out var v))
                return v;
            // fallback: hrvatski pa sam ključ (da odmah vidimo ako nešto fali)
            return _hr.TryGetValue(key, out var h) ? h : key;
        }

        // Formatirani prijevod (npr. "Dobro došao {0}").
        public static string T(string key, params object[] args) => string.Format(T(key), args);

        public static void Load(string path = "settings.json")
        {
            try
            {
                if (!File.Exists(path))
                    return;
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("Language", out var l) && l.GetString() is string lang &&
                    (lang == "hr" || lang == "en"))
                {
                    Language = lang;
                }
            }
            catch { /* Utišano - ostaje default hr */ }
        }

        private static readonly Dictionary<string, string> _hr = new()
        {
            // Zajedničko
            ["common.save"] = "Spremi",
            ["common.cancel"] = "Odustani",
            ["common.refresh"] = "Osvježi",
            ["common.add"] = "Dodaj",
            ["common.delete"] = "Obriši",
            ["common.error"] = "Greška",
            ["common.confirm"] = "Potvrda",
            ["common.info"] = "Info",

            // Login
            ["login.windowTitle"] = "Prijava - SimpleWebData Admin",
            ["login.title"] = "Sustav za upravljanje",
            ["login.apiUrl"] = "API URL:",
            ["login.username"] = "Korisničko ime:",
            ["login.password"] = "Lozinka:",
            ["login.button"] = "Prijava",
            ["login.errLogin"] = "Greška kod prijave. Pokušajte ponovno.",
            ["login.errConnect"] = "Nije moguće spojiti API: ",

            // Glavni prozor
            ["main.titleSuper"] = "Super Admin - SimpleWebData",
            ["main.titleWelcome"] = "Dobro došao {0}",
            ["main.activeSite"] = "Aktivni web site:",
            ["main.displaySize"] = "Veličina prikaza:",
            ["main.errLoadTab"] = "Greška kod učitavanja taba: {0}",
            ["main.errLoadSiteContext"] = "Greška kod učitavanja web site konteksta: {0}",
            ["main.errChangeSite"] = "Greška kod promjene web site-a: {0}",
            ["main.sessionExpiredMsg"] = "Vaša sesija je istekla. Molimo prijavite se ponovno.",
            ["main.sessionExpiredTitle"] = "Sesija istekla",
            ["main.unknown"] = "(nepoznato)",

            // Tabovi
            ["tab.pages"] = "Stranice web-a",
            ["tab.galleries"] = "Galerije & Slike",
            ["tab.facilities"] = "Objekti (Facilities)",
            ["tab.apiKey"] = "API Ključ",
            ["tab.saWebSites"] = "[SA] Web Sites",
            ["tab.saUsers"] = "[SA] Korisnici",

            // Galerije
            ["gallery.group1"] = "1. Galerije",
            ["gallery.addGallery"] = "Dodaj Galeriju",
            ["gallery.delGallery"] = "Obriši Galeriju",
            ["gallery.group2"] = "2. Slike u odabranoj galeriji",
            ["gallery.addImage"] = "Dodaj Sliku",
            ["gallery.changeImage"] = "Izmijeni Sliku",
            ["gallery.delImage"] = "Obriši Sliku",
            ["gallery.addFailed"] = "Dodavanje galerije nije uspjelo.",
            ["gallery.uploadFailed"] = "Upload slike nije uspio.",
            ["gallery.changeFailed"] = "Izmjena slike nije uspjela.",
            ["gallery.delConfirm"] = "Obrisati galeriju?",
            ["gallery.delImageConfirm"] = "Obrisati sliku?",
            ["gallery.imageFilter"] = "Slike",

            // Stranice
            ["pages.group1"] = "1. Stranice",
            ["pages.group2"] = "2. Tekstovi označene stranice",
            ["pages.addText"] = "Dodaj Tekst",
            ["pages.delText"] = "Obriši Tekst",
            ["pages.delPageConfirm"] = "Obrisati stranicu?",
            ["pages.newPageTitle"] = "Nova stranica",
            ["pages.newPageLabel"] = "Šifra stranice (Code):",
            ["pages.delTextConfirm"] = "Obrisati tekst?",
            ["pages.textDialogTitle"] = "Novi tekst stranice",
            ["pages.textCodeLabel"] = "Šifra tekst bloka (mora biti jedinstvena):",
            ["pages.textContentLabel"] = "Sadržaj (Content):",
            ["pages.colCode"] = "Šifra",
            ["pages.colSeoTitle"] = "SEO naslov",
            ["pages.colSeoDescription"] = "SEO opis",
            ["pages.colSeoKeywords"] = "SEO ključne riječi",
            ["pages.colGallery"] = "Galerija",

            // Objekti (Facilities)
            ["fac.group1"] = "1. Objekti",
            ["fac.group2"] = "2. Zauzetost odabranog objekta",
            ["fac.addDates"] = "Dodaj Raspon Datuma",
            ["fac.delDate"] = "Obriši Datum",
            ["fac.delFacConfirm"] = "Obrisati objekt?",
            ["fac.newFacTitle"] = "Novi objekt",
            ["fac.newFacLabel"] = "Šifra objekta (Code):",
            ["fac.delDateConfirm"] = "Obrisati zapis datuma?",
            ["fac.datesDialogTitle"] = "Dodaj raspon datuma",
            ["fac.fromDate"] = "Od datuma:",
            ["fac.toDate"] = "Do datuma:",
            ["fac.status"] = "Status:",
            ["fac.dateRangeError"] = "Datum DO mora biti veći ili jednak datumu OD!",

            // Web Sites (SA)
            ["sites.addSite"] = "Dodaj Web Site",
            ["sites.delSite"] = "Obriši odabrano",
            ["sites.newSiteTitle"] = "Novi Web Site",
            ["sites.codeLabel"] = "Šifra (Code):",
            ["sites.nameLabel"] = "Naziv (Name):",
            ["sites.delConfirm"] = "Obrisati odabrani Web Site?",

            // Korisnici (SA)
            ["users.addUser"] = "Dodaj Korisnika",
            ["users.delUser"] = "Obriši Korisnika",
            ["users.noSites"] = "Nema kreiranih Web Site-ova!",
            ["users.dialogTitle"] = "Novi korisnik",
            ["users.username"] = "Korisničko ime:",
            ["users.password"] = "Lozinka (Password):",
            ["users.webSite"] = "Web site:",
            ["users.isSuper"] = "Je SuperUser?",
            ["users.delConfirm"] = "Obrisati odabranog korisnika?",

            // API Ključ
            ["apikey.domainsLabel"] = "Upišite domene (odvojene zarezom):",
            ["apikey.generateBtn"] = "Generiraj Trajni Read-Only API Token",
            ["apikey.connectError"] = "Greška kod spajanja na API!",

            // Zaglavlja kolona gridova
            ["col.code"] = "Šifra",
            ["col.name"] = "Naziv",
            ["col.description"] = "Opis",
            ["col.altText"] = "Alt tekst",
            ["col.fileName"] = "Naziv datoteke",
            ["col.content"] = "Sadržaj",
            ["col.date"] = "Datum",
            ["col.status"] = "Status",
            ["col.username"] = "Korisničko ime",
            ["col.firstName"] = "Ime",
            ["col.lastName"] = "Prezime",
            ["col.email"] = "E-mail",
            ["col.isSuperUser"] = "Super korisnik",
            ["col.gallery"] = "Galerija",
            ["col.webSite"] = "Web site",
        };

        private static readonly Dictionary<string, string> _en = new()
        {
            // Common
            ["common.save"] = "Save",
            ["common.cancel"] = "Cancel",
            ["common.refresh"] = "Refresh",
            ["common.add"] = "Add",
            ["common.delete"] = "Delete",
            ["common.error"] = "Error",
            ["common.confirm"] = "Confirm",
            ["common.info"] = "Info",

            // Login
            ["login.windowTitle"] = "Login - SimpleWebData Admin",
            ["login.title"] = "Management system",
            ["login.apiUrl"] = "API URL:",
            ["login.username"] = "Username:",
            ["login.password"] = "Password:",
            ["login.button"] = "Login",
            ["login.errLogin"] = "Login failed. Please try again.",
            ["login.errConnect"] = "Cannot connect to API: ",

            // Main window
            ["main.titleSuper"] = "Super Admin - SimpleWebData",
            ["main.titleWelcome"] = "Welcome {0}",
            ["main.activeSite"] = "Active web site:",
            ["main.displaySize"] = "Display size:",
            ["main.errLoadTab"] = "Error loading tab: {0}",
            ["main.errLoadSiteContext"] = "Error loading web site context: {0}",
            ["main.errChangeSite"] = "Error changing web site: {0}",
            ["main.sessionExpiredMsg"] = "Your session has expired. Please log in again.",
            ["main.sessionExpiredTitle"] = "Session expired",
            ["main.unknown"] = "(unknown)",

            // Tabs
            ["tab.pages"] = "Web pages",
            ["tab.galleries"] = "Galleries & Images",
            ["tab.facilities"] = "Facilities",
            ["tab.apiKey"] = "API Key",
            ["tab.saWebSites"] = "[SA] Web Sites",
            ["tab.saUsers"] = "[SA] Users",

            // Galleries
            ["gallery.group1"] = "1. Galleries",
            ["gallery.addGallery"] = "Add Gallery",
            ["gallery.delGallery"] = "Delete Gallery",
            ["gallery.group2"] = "2. Images in selected gallery",
            ["gallery.addImage"] = "Add Image",
            ["gallery.changeImage"] = "Change Image",
            ["gallery.delImage"] = "Delete Image",
            ["gallery.addFailed"] = "Adding the gallery failed.",
            ["gallery.uploadFailed"] = "Image upload failed.",
            ["gallery.changeFailed"] = "Changing the image failed.",
            ["gallery.delConfirm"] = "Delete the gallery?",
            ["gallery.delImageConfirm"] = "Delete the image?",
            ["gallery.imageFilter"] = "Images",

            // Pages
            ["pages.group1"] = "1. Pages",
            ["pages.group2"] = "2. Texts of selected page",
            ["pages.addText"] = "Add Text",
            ["pages.delText"] = "Delete Text",
            ["pages.delPageConfirm"] = "Delete the page?",
            ["pages.newPageTitle"] = "New page",
            ["pages.newPageLabel"] = "Page code (Code):",
            ["pages.delTextConfirm"] = "Delete the text?",
            ["pages.textDialogTitle"] = "New page text",
            ["pages.textCodeLabel"] = "Text block code (must be unique):",
            ["pages.textContentLabel"] = "Content:",
            ["pages.colCode"] = "Code",
            ["pages.colSeoTitle"] = "SEO Title",
            ["pages.colSeoDescription"] = "SEO Description",
            ["pages.colSeoKeywords"] = "SEO Keywords",
            ["pages.colGallery"] = "Gallery",

            // Facilities
            ["fac.group1"] = "1. Facilities",
            ["fac.group2"] = "2. Occupancy of selected facility",
            ["fac.addDates"] = "Add Date Range",
            ["fac.delDate"] = "Delete Date",
            ["fac.delFacConfirm"] = "Delete the facility?",
            ["fac.newFacTitle"] = "New facility",
            ["fac.newFacLabel"] = "Facility code (Code):",
            ["fac.delDateConfirm"] = "Delete the date entry?",
            ["fac.datesDialogTitle"] = "Add date range",
            ["fac.fromDate"] = "From date:",
            ["fac.toDate"] = "To date:",
            ["fac.status"] = "Status:",
            ["fac.dateRangeError"] = "End date must be greater than or equal to start date!",

            // Web Sites (SA)
            ["sites.addSite"] = "Add Web Site",
            ["sites.delSite"] = "Delete selected",
            ["sites.newSiteTitle"] = "New Web Site",
            ["sites.codeLabel"] = "Code:",
            ["sites.nameLabel"] = "Name:",
            ["sites.delConfirm"] = "Delete the selected web site?",

            // Users (SA)
            ["users.addUser"] = "Add User",
            ["users.delUser"] = "Delete User",
            ["users.noSites"] = "No web sites created!",
            ["users.dialogTitle"] = "New user",
            ["users.username"] = "Username:",
            ["users.password"] = "Password:",
            ["users.webSite"] = "Web site:",
            ["users.isSuper"] = "Is SuperUser?",
            ["users.delConfirm"] = "Delete the selected user?",

            // API Key
            ["apikey.domainsLabel"] = "Enter domains (comma-separated):",
            ["apikey.generateBtn"] = "Generate Permanent Read-Only API Token",
            ["apikey.connectError"] = "Error connecting to API!",

            // Grid column headers
            ["col.code"] = "Code",
            ["col.name"] = "Name",
            ["col.description"] = "Description",
            ["col.altText"] = "Alt text",
            ["col.fileName"] = "File name",
            ["col.content"] = "Content",
            ["col.date"] = "Date",
            ["col.status"] = "Status",
            ["col.username"] = "Username",
            ["col.firstName"] = "First name",
            ["col.lastName"] = "Last name",
            ["col.email"] = "Email",
            ["col.isSuperUser"] = "Super user",
            ["col.gallery"] = "Gallery",
            ["col.webSite"] = "Web site",
        };
    }
}
