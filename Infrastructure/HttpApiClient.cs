// KÜTÜPHANE İMPORT'LARI
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Desktop_client_api_kod.Models;

namespace Desktop_client_api_kod.Infrastructure
{
    public sealed class HttpApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly SettingsStore _settingsStore;
        private bool _isConfigured = false;

        public HttpApiClient(SettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
            
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            
            _httpClient = new HttpClient(handler);
        }

        // ✅ DÜZELTME: BaseAddress sadece bir kez set edilecek
        private async Task ConfigureAsync()
        {
            var settings = await _settingsStore.LoadAsync();
            
            // ✅ BaseAddress'i sadece ilk seferde veya değiştiyse set et
            var newBaseUrl = settings.BaseUrl.TrimEnd('/') + "/";
            if (_httpClient.BaseAddress == null || _httpClient.BaseAddress.ToString() != newBaseUrl)
            {
                _httpClient.BaseAddress = new Uri(newBaseUrl);
                Console.WriteLine($"🌐 BaseAddress set edildi: {newBaseUrl}");
            }
            
            // Accept header
            if (!_isConfigured)
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json")
                );
            }

            // Authorization header (Bearer token)
            if (!string.IsNullOrWhiteSpace(settings.AuthToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", settings.AuthToken);
                Console.WriteLine("🎫 Bearer token eklendi");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                Console.WriteLine("⚠️ Bearer token YOK");
            }

            // X-API-Key header
            const string apiKeyHeader = "X-API-Key";
            
            if (_httpClient.DefaultRequestHeaders.Contains(apiKeyHeader))
            {
                _httpClient.DefaultRequestHeaders.Remove(apiKeyHeader);
            }
            
            // ✅ DÜZELTME: API Key'i Settings'ten al, boşsa varsayılan kullan
            var apiKey = settings.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = "84e3ea0bc8fff1c93d1b5a42f3ac91432beb01b41a827001ff53a3832f227864";
                Console.WriteLine("⚠️ Settings'te API Key yok, varsayılan kullanılıyor");
            }
            
            _httpClient.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
            Console.WriteLine($"🔑 API Key eklendi: {apiKey.Substring(0, 20)}...");
            
            _isConfigured = true;
        }

        public async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct = default)
        {
            await ConfigureAsync();
            var response = await _httpClient.GetAsync(path, ct);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
        }

        public async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
            string path, 
            TRequest body, 
            CancellationToken ct = default)
        {
            await ConfigureAsync();
            var json = JsonSerializer.Serialize(body);
            
            Console.WriteLine($"POST URL: {_httpClient.BaseAddress}{path}");
            Console.WriteLine($"Request Body: {json}");
            
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(path, content, ct);
            
            Console.WriteLine($"Response Status: {(int)response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Response Body: {responseBody}");
            
            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<TResponse>(responseBody);
        }

        public async Task<TResponse?> PostFormAsync<TResponse>(
            string path, 
            IDictionary<string, string> formFields,
            CancellationToken ct = default)
        {
            await ConfigureAsync();
            
            Console.WriteLine($"POST URL: {_httpClient.BaseAddress}{path}");
            Console.WriteLine($"Request Form: {System.Text.Json.JsonSerializer.Serialize(formFields)}");
            
            using var content = new FormUrlEncodedContent(formFields);
            var response = await _httpClient.PostAsync(path, content, ct);
            
            Console.WriteLine($"Response Status: {(int)response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Response Body: {responseBody}");
            
            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<TResponse>(responseBody);
        }

        public async Task<HttpResponseMessage> GetRawAsync(
            string path, 
            CancellationToken ct = default)
        {
            await ConfigureAsync();
            return await _httpClient.GetAsync(path, ct);
        }
    }
}