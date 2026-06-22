# SimpleWebDataClient - Priručnik za AI Agente (Frontend integracija)

Ovaj dokument je sistemska uputa namijenjena drugim AI agentima i frontend developerima koji dizajniraju statične HTML/JS web stranice. Pomoću JavaScript klase `SimpleWebDataClient` web stranica komunicira s backend REST API-jem i dohvaća sve podatke vezane uz taj tenant/sajt (tekstove, galerije slika, datume rezervacija objekata, itd.) iz SimpleWebData baze podataka.

## Inicijalizacija klijenta

Za početak rada potrebno je instancirati klijent s baznim URL-om servera i `ReadOnly` JWT tokenom koji nema datum isteka (njega korisnik generira iz WinForms admin desktop aplikacije).

```javascript
// Učitavanje klase - osigurajte da je skripta referencirana u HTML-u
// Primjer: <script src="./SimpleWebDataClient.js"></script>

const API_BASE_URL = "https://localhost:7119"; // URL backend .NET API-ja
const API_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."; // Ovdje ide ReadOnly auth token!

const dataClient = new SimpleWebDataClient(API_BASE_URL, API_TOKEN);
```

## Popis i objašnjenje metoda

### 1. `getPage(code)`
Dohvaća sav tekstualni sadržaj definiran za traženu stranicu s priloženom relacijom na fotogaleriju ako je zadana u sustavu.
- **Parametar:** `code` (string) - Jedinstvena alfanumerička šifra stranice (npr. `"o-nama"`, `"homepage"`, `"kontakt"`).
- **Vraća:** JS Objekt (`Page`) koji sadrži metapodatke stranice i listu (`texts[]`) njezinih pojedinačnih tekstualnih fragmenata.
- **Upotreba kod generiranja:** Ne hardkodirati opisne paragrafe u DOM elemente. Podatke ubacivati iteracijom iz `texts` čitajući property `content` u korelaciji s njihovim `code` imenom.

### 2. `getPhotoGallery(code)`
Dohvaća kolekciju slika unutar odabrane galerije.
- **Parametar:** `code` (string) - Kod galerije (npr. `"apartman-gallery"`).
- **Vraća:** JS Objekt s poljem `photos[]` (svaka fotka posjeduje `fileName` string te opcionalni `altText`).
- **Važno:** Metoda vraća samo "suhe" nazive datoteka slika. Za apsolutni URL potrebno je te nazive provući kroz funkciju `getImageUrl()`.

### 3. `getImageUrl(galleryCode, fileName)`
Sinkrona metoda; parsira podatke te ispisuje gotovi, čist, apsolutni REST URL slikovne datoteke direktno u HTTP tok, podobno za `img src` element.
- **Parametri:** `galleryCode` (kod izvorne galerije unutar koje slika leži) i `fileName` (ime datoteke same slike kakva stoji u bazi npr. `zgrada.jpg`).
- **Vraća:** String tipa `"https://{API_BASE_URL}/api/read/images/{galleryCode}/{fileName}"`.
- **Primjena kod izgradnje weba:** Kada se provrti `forEach()` nad fotkama dobivenim iz `getPhotoGallery()`, ovaj url ide u `<img src="X">`.

### 4. `getFacilityReservations(code, fromDate, toDate)`
Popunjava podatke za dinamički kalendar ili provjeru slobodnih termina za objekt (Facilities module u backendu).
- **Parametri:**
  - `code` (string) - Šifra objekta u adminu (npr. `"apartman-1"`).
  - `fromDate` (string, opcionalno) - Datum filter početka u obliku uobičajenom za query (npr. `2026-06-01`).
  - `toDate` (string, opcionalno) - Datum filter završetka (npr. `2026-07-01`).
- **Vraća:** Kompleksan `Facility` JSON u kojem se nalazi Array `reservations[]`. Svaki objekt unutar njega ima polje `date` (Datum) i `status` Enum podatak (`0 = Available`, `1 = Pending`, `2 = Booked`).
- **Upotreba u dizajnu:** Bojanje kalendarskih Grid elemenata s crvenom bojom za one dane gdje API ispostavi status "Booked".

---

## Primjer Integracije (Uputa za AI Agente kod izrade dizajna)

Kada korisnik zatraži da "kreiraš izgled stranice i izlistaš podatke o njoj", koristi otprilike vako spajanje u front-end JavaScript klijentskoj datoteci (tipa `app.js`):

```javascript
document.addEventListener("DOMContentLoaded", async () => {
    // 1. Priprema instanciranja klijenta. UVIJEK iskoristite ovu varijablu za data sloj.
    const client = new SimpleWebDataClient("https://localhost:7119", "DUMMY_TOKEN_ILI_KORISNIKOV");

    try {
        // --- SCENARIJ 1: Renderiranje glavnih tekstova na DOM ---
        const pageData = await client.getPage("naslovnica");
        
        // Zamišljeno je da HTML drži <div id="hero-title-container"></div> itd.
        // Izvuci točan komad teksta iz stranice tražeći njegov "code":
        const heroText = pageData.texts.find(t => t.code === "hero-title");
        if (heroText) {
            document.getElementById("hero-title-container").innerHTML = heroText.content;
        }

        // --- SCENARIJ 2: Kreiranje slidera ili DOM Grida sa Slikama ---
        const galleryInfo = await client.getPhotoGallery("vrata-apartmana-galerija");
        const galleryDOM = document.getElementById("gallery-items");
        
        galleryInfo.photos.forEach(photo => {
            const imgDom = document.createElement("img");
            imgDom.src = client.getImageUrl("vrata-apartmana-galerija", photo.fileName);
            imgDom.alt = photo.altText || "Fotografija objekta";
            
            galleryDOM.appendChild(imgDom);
        });

        // --- SCENARIJ 3: Dohvaćanje kalendarskih statusa ---
        const reservationsInfo = await client.getFacilityReservations("objekt-001");
        reservationsInfo.reservations.forEach(r => {
            // Logiramo sve "Booked" datume (rezervacijski status je redni broj 2)
            if(r.status === 2) {
                 console.log("Apartman je bukirani za datum: " + r.date);
            }
        });

    } catch (error) {
        console.error("SimpleWebData klijent iskusio je grešku!", error);
    }
});
```

---

## 4 Pravila za AI Asistente
Ovo su obavezne smjernice kad pišete novi JavaScript kod s ovim clientom!
1. **Zabranjeno je hardkodiranje "lorem ipsum" sadržaja na razinu finalne produkcije** - Preko praznih ID tagova u HTML DOM-u osigurajte mjesto. Zatim `getPage()` funkcijom ubrizgajte dinamiku u tekst, s tim da `texts.find(c => c.code)` moraju odgovarati ID postavkama iz HTML-a.
2. **Slike isključivo iz backend baze** - Nemojte koristiti nasumične vanjske slike s interneta, iterirajte kroz ugovorenu `getPhotoGallery()` listu slika i preko custom urla crtajte `<img>`.
3. **Mjeseci i Datumi** - Ako klijent traži sekciju za prikaz rezervacijskog rasporeda; obogatite DOM graf kalendara preslikom datuma unutar zatraženog `getFacilityReservations()` array-ja.
4. **Token Security** - ReadOnly token postavlja se centralno u `app.js` pri dnu instancijskog dokumenta, API podlozi dopušteno je mijenjati tokene pa mora biti unutar varijable kako bi se lako prekopirao.
