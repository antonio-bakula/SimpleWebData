# SimpleWebData

SimpleWebData is a lightweight, self-hostable backend solution designed to serve data to HTML/JavaScript static websites. It provides a REST API to fetch data features like pages, texts, photo galleries, facilities, and reservations stored in a local SQLite database.

The data administration is handled via a native Windows Forms desktop application, effectively avoiding complex web hosting procedures for an admin interface.

## Directory Structure

- **API**: A .NET Core Web API project. It provides the ReadOnly API for website consumption, and authenticated endpoints for the Admin application.
- **Admin**: A .NET WinForms Windows desktop administration application. It allows website owners to manage their content and super users to manage websites and users.
- **Client**: An HTML/JavaScript example demonstrating how to consume the ReadOnly API natively.
- **SimpleWebData.postman_collection.json**: A Postman collection containing pre-configured requests for testing the API endpoints.

## Features

- **Centralized Data Management**: Seamlessly manage multiple websites, pages, page text contents, photo galleries, and facilities with availability status.
- **Role-Based Access**:
  - *SuperUser*: Manages websites and users globally.
  - *Admin (Website Owner)*: Manages content for their specific website and generates API keys.
  - *Read-Only API*: Publicly available endpoints to retrieve data for websites. Authenticated via a stateless ReadOnly JWT token restricted by requested origins.
- **SQLite Database**: Lightweight, file-based, and easy to deploy database.

## Usage

### 1. API Server
The API server uses Minimal APIs and Entity Framework Core with SQLite. 
To start the server:
```bash
cd API
dotnet run
```
The server will automatically apply database migrations and seed initial background data.

### 2. Admin Desktop Client
The administration interface is a native Windows Forms app located in the `Admin` directory. 
- Build and run the WinForms application.
- Log in using your credentials.
- Navigate to the API Key Generator section to generate a **ReadOnly JWT token** for your website by specifying allowed domains.

### 3. JavaScript Client
The `Client` directory contains an example of how to consume the REST API from standard HTML and vanilla JavaScript.

For detailed instructions on using the JavaScript client, please refer to the [SimpleWebData ReadOnly API Javascript client](./SimpleWebDataClient-en.md) documentation.

#### Quick Start with the JS Client

Include `SimpleWebDataClient.js` in your HTML project and initialize it with your API Base URL and ReadOnly Token:

```javascript
const apiClient = new SimpleWebDataClient('http://localhost:5000', 'YOUR_READONLY_TOKEN_HERE');

// Fetch complete page data seamlessly
const pageData = await apiClient.getPage('home');

// Fetch a photo gallery and its photos
const gallery = await apiClient.getPhotoGallery('main-gallery');
```
