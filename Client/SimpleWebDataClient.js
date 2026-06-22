class SimpleWebDataClient {
    /**
     * @param {string} baseUrl Bazni URL od API-ja (npr. 'https://localhost:7119')
     * @param {string} readOnlyToken Beskonačni JWT ključ generiran od strane vlasnika web site-a
     */
    constructor(baseUrl, readOnlyToken) {
        this.baseUrl = baseUrl.replace(/\/$/, ''); // Makni zadnji slash iz stringa ako postoji
        this.readOnlyToken = readOnlyToken;
    }

    /**
     * Interna fetch metoda koja ubacuje Authorization header u svaki poziv
     */
    async _fetch(endpoint) {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${this.readOnlyToken}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`API error: ${response.status} ${response.statusText}`);
        }

        return await response.json();
    }

    /**
     * Dohvaća stranicu, njezine tekstove i fotogaleriju
     * @param {string} code Šifra stranice
     */
    async getPage(code) {
        return await this._fetch(`/api/read/pages/${code}`);
    }

    /**
     * Dohvaća fotogaleriju zajedno s podacima njenih slika
     * @param {string} code Šifra galerije
     */
    async getPhotoGallery(code) {
        return await this._fetch(`/api/read/photogalleries/${code}`);
    }

    /**
     * Dohvaća objekt i liste njegovih rezervacija
     * @param {string} code Šifra objekta (Facility)
     * @param {string} fromDate (Opcionalno) formata YYYY-MM-DD
     * @param {string} toDate (Opcionalno) formata YYYY-MM-DD
     */
    async getFacilityReservations(code, fromDate = null, toDate = null) {
        let url = `/api/read/facilities/${code}/reservations`;
        
        const params = new URLSearchParams();
        if (fromDate) params.append('from', fromDate);
        if (toDate) params.append('to', toDate);
        
        const queryString = params.toString();
        if (queryString) {
            url += `?${queryString}`;
        }

        return await this._fetch(url);
    }

    /**
     * Vraća konstruirani čisti URL prema direktnoj slici
     * @param {string} galleryCode Šifra galerije 
     * @param {string} fileName Ime datoteke iz Photo objekta
     */
    getImageUrl(galleryCode, fileName) {
        return `${this.baseUrl}/api/read/images/${galleryCode}/${fileName}`;
    }
}
