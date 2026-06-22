document.addEventListener("DOMContentLoaded", async () => {
    // NAPOMENA: Ovdje unesite port na kojem se vrti C# Aplikacija (Npr. http://localhost:5000)
    const API_BASE_URL = 'http://localhost:5072'; 
    
    let API_READ_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJXZWJTaXRlSWQiOiIxIiwiQWxsb3dlZERvbWFpbnMiOiJbXCJsb2NhbGhvc3RcIixcIjEyNy4wLjAuMVwiXSIsImV4cCI6NDkzNzgwNjMxNywiaXNzIjoiU2ltcGxlV2ViRGF0YUFwaSIsImF1ZCI6IlNpbXBsZVdlYkRhdGFDbGllbnRzIn0.FFPELDJtChxKZJWtBNO2yD3y-WSZ3YBuNzUo5MRBi9U';

    try {
        const client = new SimpleWebDataClient(API_BASE_URL, API_READ_TOKEN);

        // 1. Dohvat "home" stranice i njenih tekstova
        const pageData = await client.getPage('home');
        
        // Pomoćna funkcija za čitanje teksta po 'Code'
        const getText = (code) => pageData.texts.find(t => t.code === code)?.content || '';

        document.getElementById('page-title').innerText = getText('title') || 'Naslov nije pronađen';
        document.getElementById('page-subtitle').innerText = getText('subtitle');

        // Prikaz galerije koja je dodijeljena stranici (u seedu bi to trebao biti "gal-okolina")
        if (pageData.photoGallery && pageData.photoGallery.photos) {
            const galleryDiv = document.getElementById('gallery');
            pageData.photoGallery.photos.forEach(photo => {
                const img = document.createElement('img');
                img.src = client.getImageUrl(pageData.photoGallery.code, photo.fileName);
                img.alt = photo.altText || 'Photo';
                galleryDiv.appendChild(img);
            });
        }

        // 2. Direktni dohvat specifične Photogallery (gal-apartman) izvan Page sustava
        const standaloneGalleryData = await client.getPhotoGallery('gal-apartman');
        if (standaloneGalleryData && standaloneGalleryData.photos) {
             const standaloneDiv = document.getElementById('standalone-gallery');
             standaloneGalleryData.photos.forEach(photo => {
                const img = document.createElement('img');
                img.src = client.getImageUrl(standaloneGalleryData.code, photo.fileName);
                img.alt = photo.altText || 'Photo';
                standaloneDiv.appendChild(img);
             });
        }

        // 3. Dohvat raspoloživosti za primarni objekt 'ap1' (Apartman Sunce A1 iz našeg Seed-a)
        const facilityData = await client.getFacilityReservations('ap1');
        const resDiv = document.getElementById('reservations');
        
        const card = document.createElement('div');
        card.className = 'facility-card';
        card.innerHTML = `<h4>${facilityData.name}</h4><p>${facilityData.description || ''}</p>`;
        
        const ul = document.createElement('ul');
        if (facilityData.reservations && facilityData.reservations.length > 0) {
            facilityData.reservations.forEach(res => {
                const li = document.createElement('li');
                
                // Pretvaranje ISO datuma u lakši format (npr. DD.MM.YYYY)
                const dateStr = new Date(res.date).toLocaleDateString('hr-HR');
                
                // Bojanje statusa u CSS-u
                const cssClass = `status-${res.status.toLowerCase()}`;
                
                li.innerHTML = `Datum: <strong>${dateStr}</strong> - Status: <span class="${cssClass}">${res.status}</span>`;
                ul.appendChild(li);
            });
        } else {
            ul.innerHTML = "<li>Nema informacija o raspoloživosti.</li>";
        }
        
        card.appendChild(ul);
        resDiv.appendChild(card);

    } catch (error) {
        console.error("Greška pri dohvaćanju API-ja:", error);
        document.getElementById('page-title').innerText = "Nemoguće povezivanje s API-jem";
        document.getElementById('page-subtitle').innerText = "Provjeri je li server pokrenut, i jesi li zalijepio pravi Token u app.js! " + error.message;
    }
});