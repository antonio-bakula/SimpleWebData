using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SimpleWebDataAdmin.Models;

namespace SimpleWebDataAdmin.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private string _refreshToken = string.Empty;
        // Serijalizira refresh: ako više zahtjeva istovremeno dobije 401, samo prvi radi refresh,
        // ostali pričekaju i iskoriste novi token (refresh token se rotira pa ga ne smiju trošiti paralelno).
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public string AccessToken { get; private set; } = string.Empty;
        public UserTokenData CurrentUser { get; private set; } = new();
        public string BaseUrl => _http.BaseAddress?.ToString() ?? "";

        // Javi se kad refresh ne uspije (refresh token istekao/nevažeći) -> UI treba na ponovni login.
        public event Action? SessionExpired;

        public ApiClient(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new { Username = username, Password = password });
            if (!response.IsSuccessStatusCode)
                return false;

            var tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
            if (tokens != null)
            {
                _refreshToken = tokens.RefreshToken;
                SetToken(tokens.AccessToken);
                return true;
            }
            return false;
        }

        private void SetToken(string token)
        {
            AccessToken = token;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ParseToken(token);
        }

        private void ParseToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length > 1)
                {
                    var payload = parts[1];
                    switch (payload.Length % 4)
                    {
                        case 2: payload += "=="; break;
                        case 3: payload += "="; break;
                    }
                    
                    var jsonBytes = Convert.FromBase64String(payload);
                    var doc = JsonDocument.Parse(jsonBytes);
                    
                    CurrentUser.IsSuperUser = doc.RootElement.GetProperty("IsSuperUser").GetString() == "True";
                    CurrentUser.WebSiteId = int.Parse(doc.RootElement.GetProperty("WebSiteId").GetString() ?? "0");
                }
            }
            catch { /* Utišano za format demo grešaka */ }
        }

        // Pokuša osvježiti access token preko refresh tokena. Vraća true ako je nakon poziva
        // dostupan važeći token (ili ga je u međuvremenu osvježio drugi zahtjev).
        private async Task<bool> TryRefreshAsync(string accessTokenUsed)
        {
            await _refreshLock.WaitAsync();
            try
            {
                // Drugi zahtjev je već osvježio token dok smo čekali na lock -> samo nastavi.
                if (AccessToken != accessTokenUsed)
                    return true;

                if (string.IsNullOrEmpty(_refreshToken))
                    return false;

                using var response = await _http.PostAsJsonAsync("/api/auth/refresh",
                    new AuthTokensDto { AccessToken = AccessToken, RefreshToken = _refreshToken });

                if (!response.IsSuccessStatusCode)
                    return false;

                var tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
                if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
                    return false;

                _refreshToken = tokens.RefreshToken;
                SetToken(tokens.AccessToken);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        // Centralno slanje: na 401 (istekao access token) pokuša refresh pa ponovi zahtjev jednom.
        // Zahtjev se gradi kroz factory jer se HttpRequestMessage (i njegov sadržaj/stream) ne može slati dvaput.
        private async Task<HttpResponseMessage> SendWithRefreshAsync(Func<HttpRequestMessage> requestFactory)
        {
            var tokenUsed = AccessToken;
            var response = await _http.SendAsync(requestFactory());

            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            response.Dispose();

            if (!await TryRefreshAsync(tokenUsed))
            {
                SessionExpired?.Invoke();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return await _http.SendAsync(requestFactory());
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            using var response = await SendWithRefreshAsync(() => new HttpRequestMessage(HttpMethod.Get, url));
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>();
            return default;
        }

        public async Task<T?> PostAsync<T>(string url, object data)
        {
            using var response = await SendWithRefreshAsync(() =>
                new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(data) });
            if (response.IsSuccessStatusCode)
                 return await response.Content.ReadFromJsonAsync<T>();
            return default;
        }

        public async Task<bool> PutAsync(string url, object data)
        {
            using var response = await SendWithRefreshAsync(() =>
                new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(data) });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string url)
        {
            using var response = await SendWithRefreshAsync(() => new HttpRequestMessage(HttpMethod.Delete, url));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UploadPhotoAsync(int galleryId, string filePath, string altText)
        {
            using var response = await SendWithRefreshAsync(() => new HttpRequestMessage(
                HttpMethod.Post, $"/api/admin/photogalleries/{galleryId}/photos")
            {
                Content = BuildPhotoContent(filePath, altText)
            });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdatePhotoAsync(int photoId, string? filePath, string? altText)
        {
            using var response = await SendWithRefreshAsync(() => new HttpRequestMessage(
                HttpMethod.Put, $"/api/admin/photos/{photoId}")
            {
                Content = BuildPhotoContent(filePath, altText)
            });
            return response.IsSuccessStatusCode;
        }

        // Gradi multipart sadržaj za upload/update slike. Poziva se iznova za svaki pokušaj slanja
        // (i kod retryja nakon refresha) jer se file stream potroši slanjem pa ga treba ponovno otvoriti.
        private static MultipartFormDataContent BuildPhotoContent(string? filePath, string? altText)
        {
            var content = new MultipartFormDataContent();
            if (altText != null) content.Add(new StringContent(altText), "altText");

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var streamContent = new StreamContent(File.OpenRead(filePath));
                var ext = Path.GetExtension(filePath).ToLower();
                var mime = ext == ".png" ? "image/png" : "image/jpeg";
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
                content.Add(streamContent, "file", Path.GetFileName(filePath));
            }

            return content;
        }

        public async Task<string?> GenerateApiKeyAsync(string[] domains)
        {
            using var response = await SendWithRefreshAsync(() =>
                new HttpRequestMessage(HttpMethod.Post, "/api/admin/apikey") { Content = JsonContent.Create(domains) });
            if (response.IsSuccessStatusCode)
            {
                var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
                return doc.GetProperty("apiKey").GetString();
            }
            return null;
        }
    }
}