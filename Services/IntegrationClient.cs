using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Models;

namespace Desktop_client_api_kod.Services
{
    public sealed class IntegrationClient
    {
        private readonly HttpApiClient _api;
        private readonly SettingsStore _settingsStore;

        public IntegrationClient(HttpApiClient api, SettingsStore settingsStore)
        {
            _api = api;
            _settingsStore = settingsStore;
        }

        // PUT /integration/job/create - Dosya yükle ve sanitize başlat
        public async Task<CreateJobResponse> CreateJobsAsync(
            string apiKey,
            string batchName,
            string passwordList,
            string filePath,
            bool allowInsecureCertificates = true,
            CancellationToken ct = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Dosya bulunamadı", filePath);
            }

            var settings = await _settingsStore.LoadAsync();
            if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                throw new InvalidOperationException("BaseUrl ayarlanmadı. Önce hostname belirleyin.");
            }

            var normalizedBase = settings.BaseUrl.TrimEnd('/') + "/";

            var handler = new HttpClientHandler();
            if (allowInsecureCertificates)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            using var http = new HttpClient(handler)
            {
                BaseAddress = new Uri(normalizedBase)
            };

            http.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(batchName), "batch_name");
            if (!string.IsNullOrWhiteSpace(passwordList))
            {
                form.Add(new StringContent(passwordList), "password_list");
            }

            await using var fs = File.OpenRead(filePath);
            using var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            form.Add(fileContent, "attachments", Path.GetFileName(filePath));

            Console.WriteLine($"PUT URL: {normalizedBase}integration/job/create");
            Console.WriteLine($"Batch Name: {batchName}");
            Console.WriteLine($"File: {Path.GetFileName(filePath)}");

            var response = await http.PutAsync("integration/job/create", form, ct);
            
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Response Status: {(int)response.StatusCode}");
            Console.WriteLine($"Response Body: {responseBody}");
            
            response.EnsureSuccessStatusCode();
            
            return JsonSerializer.Deserialize<CreateJobResponse>(responseBody);
        }

        // ✅ DÜZELTME: Basitleştirilmiş job history çekme
        public async Task<JobHistoryResponse> GetJobHistoryAsync(CancellationToken ct = default)
        {
            try
            {
                Console.WriteLine("\n📊 JOB HISTORY YÜKLENIYOR...");
                
                var settings = await _settingsStore.LoadAsync();
                Console.WriteLine($"🌐 Base URL: {settings.BaseUrl}");
                Console.WriteLine($"🔑 API Key: {(string.IsNullOrEmpty(settings.ApiKey) ? "YOK ❌" : "✅ VAR")}");
                Console.WriteLine($"🎫 Auth Token: {(string.IsNullOrEmpty(settings.AuthToken) ? "YOK ❌" : "✅ VAR")}");
                
                var endpoint = "integration/job/list";
                Console.WriteLine($"📡 Endpoint: {endpoint}");
                
                var response = await _api.GetRawAsync(endpoint, ct);
                var statusCode = (int)response.StatusCode;
                var json = await response.Content.ReadAsStringAsync(ct);
                
                Console.WriteLine($"📊 HTTP Status: {statusCode}");
                Console.WriteLine($"📄 Response ({json.Length} chars): {json.Substring(0, Math.Min(500, json.Length))}...");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ API başarısız: {statusCode}");
                    return null;
                }
                
                var result = JsonSerializer.Deserialize<JobHistoryResponse>(json);
                
                if (result?.data != null && result.data.Count > 0)
                {
                    Console.WriteLine($"✅ {result.data.Count} job bulundu!");
                    foreach (var item in result.data)
                    {
                        Console.WriteLine($"   📄 {item.user_job_info.file_name} - {item.user_job_info.status}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Hiç job bulunamadı (data count: {result?.data?.Count ?? 0})");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Job history hatası: {ex.GetType().Name}");
                Console.WriteLine($"   Mesaj: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        // GET /integration/job/download/original/{id}
        public Task<HttpResponseMessage> DownloadOriginalAsync(string id, CancellationToken ct = default)
        {
            return _api.GetRawAsync($"integration/job/download/original/{id}", ct);
        }

        // GET /integration/job/download/sanitized/{id}
        public Task<HttpResponseMessage> DownloadSanitizedAsync(string id, CancellationToken ct = default)
        {
            return _api.GetRawAsync($"integration/job/download/sanitized/{id}", ct);
        }

        // GET /integration/job/status/{id}
        public Task<HttpResponseMessage> GetJobStatusAsync(string id, CancellationToken ct = default)
        {
            return _api.GetRawAsync($"integration/job/status/{id}", ct);
        }

        // GET /integration/job/{user_job_id}
        public Task<HttpResponseMessage> GetOperationsAsync(string userJobId, CancellationToken ct = default)
        {
            return _api.GetRawAsync($"integration/job/{userJobId}", ct);
        }

        // GET /integration/pdf-report/{user_job_id}
        public Task<HttpResponseMessage> DownloadPdfReportAsync(string userJobId, CancellationToken ct = default)
        {
            return _api.GetRawAsync($"integration/pdf-report/{userJobId}", ct);
        }
    }
}