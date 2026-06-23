# SimpleWebData API projekt
API preko kojega mogu dohvatiti podatke za javascript kod, ideja je da imam način kako da definiram podatke koje bi onda koristio na html / javascript web site-ovima. Taj API bi podakte čitao iz lokalne SQLite baze podataka. Ideja je da preko API-a se definira samo dohvat podataka a sve ostalo je na html web-u kojega bi kreirao AI agent.

## Struktura podataka
WebSites - opis web site-a, ima polja id, šifra, opis, lista usera
Users - id, username, password, firstname, lastname, email, isSuperUser
Photogalleries - jedna fotogalerija je skupina fotografija, za svaku fotografiju trebam podatke: id, blob sa slikom, alt text, filename. Za fotogalerije trebam id, relacija na website, šifra, naziv fotogalerije, opis fotogalerije
Reservations - podaci o rezervacijama soba ili općenito objekata, za svaki objekt trebam podatke po datumima da li su: slobodni, rezervacija u tijeku, zauzeti. Za objekt trebam id, relacija na website, šifra, naziv, opis, relacija na fotogaleriju
Pages - sadrži podatke o tekstovima koji su na jednoj web stranici, smisao je da se u jednom requestu dohvate svi tekstovi za stranicu. Svaki zapis o tekstu ima podatke: id, relacija na website, šifra, tekst a web stranica sadrži podatke: id, šifra, lista tekstova, relacija na fotogaleriju

## Entity modeli

```csharp
using System;
using System.Collections.Generic;

namespace SimpleWebData.Models
{
	public class WebSite
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty;
		public string? Description { get; set; }

		public ICollection<User> Users { get; set; } = new List<User>();
		public ICollection<PhotoGallery> PhotoGalleries { get; set; } = new List<PhotoGallery>();
		public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
		public ICollection<Page> Pages { get; set; } = new List<Page>();
		public ICollection<PageText> PageTexts { get; set; } = new List<PageText>();
	}

	public class User
	{
		public int Id { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? Email { get; set; }
				public bool IsSuperUser { get; set; }

		public int WebSiteId { get; set; }
		public WebSite WebSite { get; set; } = null!;
	}

	public class PhotoGallery
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }

		public int WebSiteId { get; set; }
		public WebSite WebSite { get; set; } = null!;

		public ICollection<Photo> Photos { get; set; } = new List<Photo>();
	}

	public class Photo
	{
		public int Id { get; set; }
		public byte[] ImageData { get; set; } = Array.Empty<byte>();
		public string? AltText { get; set; }
		public string FileName { get; set; } = string.Empty;

		public int PhotoGalleryId { get; set; }
		public PhotoGallery PhotoGallery { get; set; } = null!;
	}

	public class Facility
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }

		public int WebSiteId { get; set; }
		public WebSite WebSite { get; set; } = null!;

		public int? PhotoGalleryId { get; set; }
		public PhotoGallery? PhotoGallery { get; set; }

		public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
	}

	public enum ReservationStatus
	{
		Available,
		Pending,
		Booked
	}

	public class Reservation
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public ReservationStatus Status { get; set; } = ReservationStatus.Available;

		public int FacilityId { get; set; }
		public Facility Facility { get; set; } = null!;
	}

	public class Page
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty;

		public int WebSiteId { get; set; }
		public WebSite WebSite { get; set; } = null!;

		public int? PhotoGalleryId { get; set; }
		public PhotoGallery? PhotoGallery { get; set; }

		public ICollection<PageText> Texts { get; set; } = new List<PageText>();
	}

	public class PageText
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;

		public int WebSiteId { get; set; }
		public WebSite WebSite { get; set; } = null!;

		public int PageId { get; set; }
		public Page Page { get; set; } = null!;
	}
}
```

# API Metode
API ćemo podijeliti više cjelina:
- ReadOnly API - koriste ga html / javascript web site-ovi za dohvat podataka
- UserAdmin API - koristi ga vlasnik html site-a da definira podatke za web stranicu
- SuperUserAdmin API - kojega koristi super admin za administraciju web site-ova i usera

Autentikacija ovisi o cjelini, na ReadOnly API se spaja tako da se u header stavi JWT token koji NEMA vrijeme isteka i u njemu su navedene domene sa kojih se može zvati API, UserAdmin API treba imati metodu koja generira taj token a u parametru se šalje lista domena. Za UserAdmin i SuperUserAdmin auth se koristi Login metoda koja ima parametre username / password i koja vraća kratkotrajni JWT token (npr. 10 min), i refresh token. Mora imati metodu za refresh tokena. Zatim za sve metode UserAdmin i SuperUserAdmin API-a se šalje bearer token u Authorization headeru, naravno za SuperUserAPI sustav mora provjeriti da li je korisnik usper user, tj. ta informacija treba pisati u tokenu.

## Popis API metoda

### Auth API (Javno dostupno)
- `POST /api/auth/login` - Prima `username` i `password`, vraća kratkotrajni JWT (10 min) i `refreshToken`.
- `POST /api/auth/refresh` - Prima `refreshToken`, vraća novi kratkotrajni JWT i novi `refreshToken`.

### ReadOnly API (Auth: JWT bez isteka iz headera)
*Klijent šalje ReadOnly JWT. Sustav validira potpis tokena i provjerava je li `Origin` ili `Referer` requesta naveden u listi dozvoljenih domena unutar tokena.*
- `GET /api/read/pages/{code}` - Vraća podatke o stranici, listu tekstova i povezanu fotogaleriju sa slikama.
- `GET /api/read/photogalleries/{code}` - Vraća fotogaleriju sa svim njenim slikama (vraća URL-ove slika radi boljih performansi i cacheiranja).
- `GET /api/read/images/{galleryCode}/{fileName}` - Vraća samu binarnu datoteku slike iz baze (URL prilagođen za SEO). `FileName` mora biti jedinstven unutar jedne fotogalerije.
- `GET /api/read/facilities/{code}/reservations` - Vraća objekt i listu rezervacija (opcionalno filtrirano po rasponu datuma).

### UserAdmin API (Auth: Kratkotrajni JWT, Role: User)
*Sve akcije su automatski ograničene na `WebSiteId` korisnika koji je ulogiran.*
- **API Keys**
  - `POST /api/admin/apikey` - Prima listu domena (`string[]`), u payload tokena zapisuje domene i `WebSiteId`, te vraća generirani ReadOnly JWT bez datuma isteka.
- **PhotoGalleries i Photos**
  - `GET /api/admin/photogalleries` - Vrijedi za ulogirani WebSite.
  - `POST /api/admin/photogalleries`
  - `PUT /api/admin/photogalleries/{id}`
  - `DELETE /api/admin/photogalleries/{id}`
  - `POST /api/admin/photogalleries/{id}/photos` - Upload novih slika.
  - `DELETE /api/admin/photos/{id}` - Brisanje slike.
- **Facilities i Reservations**
  - `GET /api/admin/facilities`
  - `POST /api/admin/facilities`
  - `PUT /api/admin/facilities/{id}`
  - `DELETE /api/admin/facilities/{id}`
  - `GET /api/admin/facilities/{id}/reservations`
  - `POST /api/admin/facilities/{id}/reservations`
  - `PUT /api/admin/reservations/{id}`
  - `DELETE /api/admin/reservations/{id}`
- **Pages i PageTexts**
  - `GET /api/admin/pages`
  - `POST /api/admin/pages`
  - `PUT /api/admin/pages/{id}`
  - `DELETE /api/admin/pages/{id}`
  - `GET /api/admin/pages/{id}/texts`
  - `POST /api/admin/pages/{id}/texts`
  - `PUT /api/admin/pagetexts/{id}`
  - `DELETE /api/admin/pagetexts/{id}`

### SuperUserAdmin API (Auth: Kratkotrajni JWT, Role: SuperUser)
*Imaju pristup svim entitetima sustava.*
- **WebSites**
  - `GET /api/superadmin/websites`
  - `POST /api/superadmin/websites`
  - `PUT /api/superadmin/websites/{id}`
  - `DELETE /api/superadmin/websites/{id}`
- **Users**
  - `GET /api/superadmin/users`
  - `POST /api/superadmin/users` - Dodavanje usera (mora se dodijeliti `WebSiteId`).
  - `PUT /api/superadmin/users/{id}`
  - `DELETE /api/superadmin/users/{id}`

# .NET Core API project SimpleWebData
U direktoriju ./src napraviti ćeš .NET Core WEB API project sa Entity Framework-om i entitetima napravljenim po definiciji baze podataka koju smo definirali iznad. Projekt neka se zove SimpleWebData, isto tako nazovi i bazu.

## Plan implementacije

1. **Inicijalizacija projekta**: 
   - Kreiranje ASP.NET Core Web API projekta pod nazivom `SimpleWebData` u folderu `./src` (koristeći Minimal API).

2. **Dodavanje NuGet paketa**: 
   - Entity Framework Core za SQLite (`Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.EntityFrameworkCore.Design`).
   - Autentikacija i autorizacija (`Microsoft.AspNetCore.Authentication.JwtBearer`).

3. **Kreiranje Entiteta (Models)**: 
   - Dodavanje ranije definiranih C# klasa za podatke (`WebSite`, `User`, `PhotoGallery`, `Photo`, `Facility`, `Reservation`, `Page`, `PageText`).

4. **Postavljanje DbContext-a**: 
   - Kreiranje `AppDbContext` klase.
   - Definiranje kompozitnih unique ključeva i kaskadnog brisanja u `OnModelCreating` (npr. jedinstveni ključ za `PhotoGalleryId` i `FileName` na slici).

5. **Konfiguracija Servisa (Program.cs i appsettings.json)**: 
   - Uključivanje SQLite konekcije.
   - Registracija CORS politike za frontend klijente.
   - Postavljanje JWT postavki (Issuer, Audience, SecretKey) i dodavanje Auth middlewarea u pipeline.

6. **Migracije i uvodni podaci (Seeding)**: 
   - Pokretanje inicijalne migracije kako bi se kreirala datoteka baze podataka.
   - Implementacija kratkog "Seedera" koji će pri pokretanju (ako je baza prazna) dodati:
	 - Jednog `SuperUser-a` (admina) za početak,
	 - Jedan probni `WebSite` i dodijeljenog regularnog vlasnika (User admina),
	 - 2 fotogalerije gdje bi svaka sadržavala po nekoliko slika,
	 - 2 objekta (`Facilities`) i njihove simulirane datume rezervacija,
	 - 2 stranice sa svojim redovima tesksta (npr. *Naslovnica* i *Kontakt*).

7. **Definiranje DTO klasa (Data Transfer Objects)**: 
   - Strukturiranje objekata za response (npr. `PhotoGalleryDto`, `AuthResponse`) kako bi se sakrili nebitni podaci (poput password hasha) i spriječile EF Core greške kod cirkularnih referenci.

8. **Implementacija Auth API metoda**: 
   - Logika za verifikaciju lozinki i izdavanje kratkotrajnog JWT tokena (s claimovima za WebSiteId i IsSuperUser).
   - Generiranje i validacija Refresh tokena.

9. **Implementacija ReadOnly API metoda**: 
   - Postavljanje custom autorizacije koja parsira dugotrajni JWT.
   - Provjera validnosti domena pozivatelja na osnovu onih izlistanih u tokenu (preko `Origin` ili `Referer` headera).
   - Endpointi za GET podataka (stranice, tekstovi, rezervacije).
   - Serviranje slike direktno iz njezina `ImageData` bloba na endpointu `/api/read/images/{galleryCode}/{fileName}`.

10. **Implementacija UserAdmin API metoda**: 
	- Osiguranje ruta podauth-om koji zahtijeva uobičajen JWT.
	- Metoda za kreiranje ReadOnly API tokena za domenu klijenta.
	- Implementacija CRUD manipulacija podacima uz izričito filtriranje: svaka akcija mora uzimati u obzir operaciju nad `WebSiteId == User.Claims.WebSiteId`.

11. **Implementacija SuperUserAdmin API metoda**: 
	- Zaključavanje ovih ruta role-based (claim-based) filterom za SuperUser-a.
	- Dohvat i CRUD upravljanje `WebSite`-ovima i `User`-ima.


# Example html / javascript projekt

Treba nam i primjer korištenja našega API-a dakle samo sekcija ReadOnly API, svakako bi napravio jedan Javascript object koji bi olakšao korištenje API-a.
Njega ćemo dodati u ./js direktorij

## SimpleWebData Javascript client za ReadOnly API

### Plan implementacije JS klijenta

1. **Kreiranje osnove klijenta (`SimpleWebDataClient.js`)**:
   - Smještaj datoteke u direktorij `./js`.
   - Klasa `SimpleWebDataClient` čiji konstruktor prima osnovne parametre: `baseUrl` API-ja i `readOnlyToken`.
   - Skrivena lokalna funkcija zasnovana na `fetch` API-ju koja na svaki REST poziv automatski injektira JWT token u HTTP headere.

2. **Implementacija metoda u JavaScript klijentu**:
   - `async getPage(code)`: Komunicira s `GET /api/read/pages/{code}`.
   - `async getPhotoGallery(code)`: Komunicira s `GET /api/read/photogalleries/{code}` i obrađuje JSON objekt galerije.
   - `async getFacilityReservations(code)`: Komunicira s `GET /api/read/facilities/{code}/reservations`.
   - `getImageUrl(galleryCode, fileName)`: Pomoćna (sinkrona) metoda koja samo konstruira apsolutni URL do slike (npr. `http://api.../images/{galleryCode}/{fileName}`).

3. **Izrada demo HTML primjera (`index.html`)**:
   - Postavljanje u direktorij `./js`.
   - Primjer strukturiranog HTML-a s praznim elementima (placeholderima) kao što su `<div id="page-content"></div>`, `<div id="gallery"></div>`.
   - Import JS klijentskog koda (bez upotrebe složenog build sustava, obično referenciranje klasa preko `<script>` oznake).

4. **Konzumacija API-ja iz demo koda (`app.js` ili unutar tagova)**:

## Plan testiranja
1. **Pokretanje API servera i baze**
   - Otvori terminal u direktoriju `./src/SimpleWebData` i pokreni `dotnet run`.
   - Pričekaj da se server podigne i u konzoli zabilježi na kojem URL-u i portu aplikacija sluša (najčešće `http://localhost:5000`). Server će automatski kreirati bazu, popuniti korisnike i testne podatke.
2. **Postavljanje i pokretanje JS Dema**
   - Otvori datoteku `./js/app.js` i provjeri je li `API_BASE_URL` varijabla odgovara URL-u s prethodnog koraka.
   - Pomoću VS Code ekstenzije (npr. *Live Server*) posluži datoteku `./js/index.html`.
   - **Automatizirano spajanje:** Klijentski kod će se sam za potrebe dema u pozadini ulogirati u admin račun, zatražiti trajni API (ReadOnly) ključ i njime inicijalizirati klijent pomoću kojega vadi podatke na zaslon.
3. **Provjera (ili ručno REST testiranje)**
   - Pogledaj prikazanu web stranicu - moraju biti izlistani "Apartman Sunce" i placeholder fotografije iz API baze.
   - *Opcionalno:* Za dodatno testiranje, otvori Postman i poigraj se korištenjem `superadmin`/`123` podataka.

# .NET Core WinForms project SimpleWebDataAdmin
Kako bi se obišli problemi i smanjili troškovi hostinga klasičnog web baziranog administracijskog sučelja, za administraciju sustava izradit će se native Windows Forms desktop aplikacija. Ova aplikacija komunicira s podacima isključivo putem našeg REST API-ja.

Zamišljena je kao jedinstvena "client" aplikacija koja interno prilagođava svoje sučelje (UI) obzirom na to tko se prijavio:
- **Obični Admin (Vlasnik weba)**: Ima pristup isključivo repozitorijima svog `WebSite`-a. Vidi module za upravljanje galerijama, objektima, rezervacijama, stranicama i tekstovima, te dobiva sučelje za generiranje svog trajnog "ReadOnly" API tokena.
- **Super Admin**: Ima apsolutno sve mogućnosti običnog admina za vlastiti site, ali mu se u glavnom sučelju dodatno otključavaju moduli (tabovi/izbornici) za globalno upravljanje `WebSite`-ovima i sistemskim `User`-ima.

## Arhitektura i Tehnologije
- **UI Framework**: Windows Forms na novijem .NET stacku (.NET 8, 9 ili 10).
- **Mrežni sloj**: `HttpClient` s centraliziranim *wrapperom* (npr. `ApiClient` klasa) za ubrizgavanje Bearer JWT tokena u svaki zahtjev.
- **Prikaz podataka**: WinForms kontrole poput `DataGridView` za tabularne podatke, klasične kontrole za unos detalja, te `PictureBox` / `OpenFileDialog` komponente za preview i uvoz slika prije slanja na server.

## Plan implementacije

1. **Inicijalizacija projekta**:
   - Kreiranje novog Windows Forms App projekta u `./src/SimpleWebDataAdmin`.

2. **Autentikacija, sesija i state management**:
   - **Login Forma**: Inicijalni iskočni prozor s obaveznim upisom `Username` i `Password`.
   - Prijava se ostvaruje pozivom `POST /api/auth/login`.
   - Uspješan odgovor, koji sadrži `AccessToken` i `RefreshToken`, upisuje se u globalnu klasu (npr. `ApplicationState`). Iz samog tokena ili API odgovora izvlači se pripadajuća uloga (`IsSuperUser`).

3. **Izrada HTTP klijenta (API Wrapper)**:
   - Razvoj utility klase `ApiClient` koja će automatski procesirati centralizirane greške (poput `401 Unauthorized`), u pozadini automatski potegnuti mehanizam rotacije ključa preko `/api/auth/refresh`, te nakon obnove tokena transparentno ponoviti originalni zahtjev kako bi se očuvao fluidni korisnički doživljaj (bez smrzavanja ili prisilnog logouta).

4. **Glavni prozor aplikacije (Main Form)**:
   - Implementacija "Dashboarda" temeljenog na `TabControl` ili sučelju s lijevim navigacijskim menijem koji učitava odvojene "User Controls" kao ekrane.
   - Dinamičko instanciranje UI grana sistemskog admina, izričito ovisno o spomenutoj varijabli `IsSuperUser`.

5. **Implementacija SuperAdmin Modula**:
   - **WebSites UI**: Forma koja sadrži DataGridView svih sajtova, s jednostavnim formama (pop-up ili paneli pokraj crteža) za dodavanje ili uređivanje naziva organizacija/webova.
   - **Users UI**: Sličan modul za stvaranje klijenata, definiciji "tko je super admin", dodjeli inicijalnih lozinki, te neophodnom povezivanju usera za referentni `WebSiteId` s padajućeg popisa (ComboBox) sajtova.

6. **Implementacija UserAdmin Modula (Temeljni upravljački sloj)**:
   - **Galerije i Slike (PhotoGalleries & Photos)**: Master-detail struktura; odabir galerije lijevo puni grid fotkama s desne strane. Korištenje `MultipartFormDataContent` u C#-u za integraciju gumba "Dodaj slike", s otvaranjem file dialoga na stolnom računalu klijenta i uploadom.
   - **Objekti i Rezervacije (Facilities & Reservations)**: Sučelje koje prvo nudi definiciju ponude objekta/apartmana; a klik u detalje tog objekta prikazuje listu/panel sa svim pripadajućim rezervacijama i datumima po `Status` logici.
   - **Stranice i Tekstovi (Pages & PageTexts)**: Hijerarhijski dizajn (dropdown za odabir stranice, ispod njega grid tekstova pripadajuće stranice), gdje poduzeća lako i pregledno definiraju sav sadržaj za `API_READ`.

7. **API Key Generator**:
   - Manji administratorski tab sa tekstualnim ulazom za domene (npr. linija po linija).
   - Pozivanje API rute `/api/admin/apikey` te omogućavanje korisne "Copy to Clipboard" tipke kojom će administrator prenijeti besmrtni "ReadOnly" token u JavaScript svoga finalnog websajta.


## Dorade na admin app

- općenito iz svih gridova makni WebSiteId i Id, to su čisto tehnički podatci
- main form treba pamtiti svoju poziciju i veličinu

**1. Galerije**
- neka učita galerije odmah po prikazu TAB-a, gumb "Očitaj Galerije" rename u "Osvježi"
- grid galerija treba biti 1/3 client prostora parenta a Grid slika 2/3
- omogućiti dodavanje i brisanje galerija
- omogućiti editiranje podataka galerije u gridu (inplace edit)
- promjena aktivne galerije treba prikazati slike u istoj u donjem gridu, to ne radi
- omogućiti editiranje slika i ponovni upload slike
- omogućiti dodavanje i brisanje slika
- panel u kojem bi se prikazivala fotografija. recimo 2/3 trenutne širine da je grid 1/3 fotografija koja se prikazuje inicijalno i kada se mijenja odabrani zapis u gridu fotografija

**2. Objekti (Facilities)**
- neka učita objekte odmah po prikazu taba
- omogućiti editiranje stanja zauzetosti apartmana u gridu sa listom datuma
- dodati mogućnost dodavanja range-a datuma u grid sa listom datuma
- omogućiti mijenjanje stanja zauzetosti za datum u gridu 
- veličina grida sa objektima 1/4, grid sa datumima 3/4 client prostora parenta
- umjesto Id-a fotogalerije prikazati Code
- omogućiti editiranje podatka objekta u gridu (inplace edit)
- disablirati da se gridovima pojavljuje onaj dodatni prazni row, to je valjda za novi zapis ali mi imamo gumb

**3. Stranice web-a**
- omogućiti edit podataka stranice u gridu (inplace edit)
- dodati grid sa tekstovima pojedine stranice, popis stranica 1/3 veličine client prostora parenta, tekstovi 2/3
- omogućiti edit tekstova u gridu (inplace edit), dodavanje novog teksta (paziti da je Code različit)
- disablirati da se gridovima pojavljuje onaj dodatni prazni row, to je valjda za novi zapis ali mi imamo gumb

**4. [SA] Web sites**
- automatski učitati podatke web site-ova po prikazu taba
- omogućiti dodavanje novoga web site-a
- omogućiti editiranje podataka web site-a
- disablirati da se gridovima pojavljuje onaj dodatni prazni row, to je valjda za novi zapis ali mi imamo gumb

**5. [SA] Korisnici**
- automatski učitati korisnike po prikazu taba
- omogućiti editiranje korisnika u gridu (inplace edit)
- umjesto da se unosi web site Id, treba biti dropdown sa Code site-a
- disablirati da se gridovima pojavljuje onaj dodatni prazni row, to je valjda za novi zapis ali mi imamo gumb