# SimpleWebDataClient - Manual for AI Agents (Frontend Integration)

This document is a system guide intended for other AI agents and frontend developers designing static HTML/JS websites. Using the JavaScript class `SimpleWebDataClient`, the website communicates with the backend REST API and fetches all data related to that tenant/site (texts, image galleries, facility reservation dates, etc.) from the SimpleWebData database.

## Client Initialization

To get started, it is necessary to instantiate the client with the server's base URL and a `ReadOnly` JWT token that has no expiration date (users generate this from the WinForms admin desktop application).

```javascript
// Load class - ensure the script is referenced in the HTML
// Example: <script src="./SimpleWebDataClient.js"></script>

const API_BASE_URL = "https://localhost:7119"; // URL of the backend .NET API
const API_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."; // ReadOnly auth token goes here!

const dataClient = new SimpleWebDataClient(API_BASE_URL, API_TOKEN);
```

## List and Explanation of Methods

### 1. `getPage(code)`
Retrieves all text content defined for the requested page with the attached photo gallery relation if specified in the system.
- **Parameter:** `code` (string) - Unique alphanumeric page code (e.g., `"about-us"`, `"homepage"`, `"contact"`).
- **Returns:** JS Object (`Page`) containing the page metadata and a list (`texts[]`) of its individual text fragments.
- **Usage in generation:** Do not hardcode descriptive paragraphs into DOM elements. Inject data by iterating from `texts` and reading the `content` property correlated with their `code` name.

### 2. `getPhotoGallery(code)`
Retrieves the collection of images within the selected gallery.
- **Parameter:** `code` (string) - Gallery code (e.g., `"apartment-gallery"`).
- **Returns:** JS Object with a `photos[]` array (each photo has a `fileName` string and optional `altText`).
- **Important:** The method returns only "raw" image file names. To get the absolute URL, these names must be passed through the `getImageUrl()` function.

### 3. `getImageUrl(galleryCode, fileName)`
Synchronous method; parses the data and returns a ready, clean, absolute REST URL of the image file, suitable for the `img src` element.
- **Parameters:** `galleryCode` (the code of the parent gallery where the image lies) and `fileName` (the filename of the image as it is in the database, e.g., `building.jpg`).
- **Returns:** A string like `"https://{API_BASE_URL}/api/read/images/{galleryCode}/{fileName}"`.
- **Application in web building:** When running `forEach()` over photos obtained from `getPhotoGallery()`, put this url into an `<img src="X">`.

### 4. `getFacilityReservations(code, fromDate, toDate)`
Populates data for a dynamic calendar or checks available dates for a facility (Facilities module in the backend).
- **Parameters:**
  - `code` (string) - Facility code in the admin (e.g., `"apartment-1"`).
  - `fromDate` (string, optional) - Start date filter, standard query format (e.g., `2026-06-01`).
  - `toDate` (string, optional) - End date filter (e.g., `2026-07-01`).
- **Returns:** A complex `Facility` JSON containing a `reservations[]` array. Each object inside has a `date` (Date) field and `status` Enum data (`0 = Available`, `1 = Pending`, `2 = Booked`).
- **Usage in design:** Coloring calendar Grid elements with red for those days where the API returns the "Booked" status.

---

## Integration Example (Instruction for AI Agents during design)

When a user asks you to "create the layout of the page and list its data", use roughly this connection approach in the front-end JavaScript client file (e.g., `app.js`):

```javascript
document.addEventListener("DOMContentLoaded", async () => {
    // 1. Prepare client instantiation. ALWAYS use this variable for the data layer.
    const client = new SimpleWebDataClient("https://localhost:7119", "DUMMY_OR_USER_TOKEN");

    try {
        // --- SCENARIO 1: Rendering main texts to the DOM ---
        const pageData = await client.getPage("homepage");
        
        // It's intended that the HTML has a <div id="hero-title-container"></div> etc.
        // Extract the exact text piece from the page by searching for its "code":
        const heroText = pageData.texts.find(t => t.code === "hero-title");
        if (heroText) {
            document.getElementById("hero-title-container").innerHTML = heroText.content;
        }

        // --- SCENARIO 2: Creating a slider or DOM Grid with Images ---
        const galleryInfo = await client.getPhotoGallery("apartment-door-gallery");
        const galleryDOM = document.getElementById("gallery-items");
        
        galleryInfo.photos.forEach(photo => {
            const imgDom = document.createElement("img");
            imgDom.src = client.getImageUrl("apartment-door-gallery", photo.fileName);
            imgDom.alt = photo.altText || "Facility photograph";
            
            galleryDOM.appendChild(imgDom);
        });

        // --- SCENARIO 3: Fetching calendar statuses ---
        const reservationsInfo = await client.getFacilityReservations("facility-001");
        reservationsInfo.reservations.forEach(r => {
            // We log all "Booked" dates (reservation status is the numeric value 2)
            if(r.status === 2) {
                 console.log("Apartment is booked for date: " + r.date);
            }
        });

    } catch (error) {
        console.error("SimpleWebData client encountered an error!", error);
    }
});
```

---

## 4 Rules for AI Assistants
These are mandatory guidelines when writing new JavaScript code with this client!
1. **Hardcoding "lorem ipsum" content in the final production is forbidden** - Provide space using empty ID tags in the HTML DOM. Then use the `getPage()` function to inject text dynamically, ensuring that `texts.find(c => c.code)` matches the ID configurations from HTML.
2. **Images exclusively from the backend database** - Do not use random external images from the internet; iterate through the retrieved `getPhotoGallery()` image list and draw `<img>` tags via the custom url.
3. **Months and Dates** - If the client requests a section to display the reservation schedule, enrich the calendar DOM graph by mapping dates within the fetched `getFacilityReservations()` array.
4. **Token Security** - The ReadOnly token is set centrally in `app.js` toward the top of the instance file; the API layer is allowed to change tokens, so it must be contained in a variable for easy copying.
