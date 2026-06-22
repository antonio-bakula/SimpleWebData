using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using SimpleWebDataAdmin.Models;

namespace SimpleWebDataAdmin.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        public string AccessToken { get; private set; } = string.Empty;
        public UserTokenData CurrentUser { get; private set; } = new();
        public string BaseUrl => _http.BaseAddress?.ToString() ?? "";

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

        public async Task<T?> GetAsync<T>(string url)
        {
            var response = await _http.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>();
            return default;
        }

        public async Task<T?> PostAsync<T>(string url, object data)
        {
            var response = await _http.PostAsJsonAsync(url, data);
            if (response.IsSuccessStatusCode)
                 return await response.Content.ReadFromJsonAsync<T>();
            return default;
        }
        
        public async Task<bool> PutAsync(string url, object data)
        {
            var response = await _http.PutAsJsonAsync(url, data);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string url)
        {
            var response = await _http.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UploadPhotoAsync(int galleryId, string filePath, string altText)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(altText ?? ""), "altText");
            
            var fileStream = File.OpenRead(filePath);
            var streamContent = new StreamContent(fileStream);
            
            var ext = Path.GetExtension(filePath).ToLower();
            var mime = ext == ".png" ? "image/png" : "image/jpeg";
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
            
            content.Add(streamContent, "file", Path.GetFileName(filePath));

            var response = await _http.PostAsync($"/api/admin/photogalleries/{galleryId}/photos", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdatePhotoAsync(int photoId, string? filePath, string? altText)
        {
            using var content = new MultipartFormDataContent();
            if (altText != null) content.Add(new StringContent(altText), "altText");
            
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var fileStream = File.OpenRead(filePath);
                var streamContent = new StreamContent(fileStream);
                var ext = Path.GetExtension(filePath).ToLower();
                var mime = ext == ".png" ? "image/png" : "image/jpeg";
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
                content.Add(streamContent, "file", Path.GetFileName(filePath));
            }

            var response = await _http.PutAsync($"/api/admin/photos/{photoId}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<string?> GenerateApiKeyAsync(string[] domains)
        {
            var response = await _http.PostAsJsonAsync("/api/admin/apikey", domains);
            if (response.IsSuccessStatusCode)
            {
                var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
                return doc.GetProperty("apiKey").GetString();
            }
            return null;
        }
    }
}