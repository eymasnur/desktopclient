using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Models;

namespace Desktop_client_api_kod.Services
{
    // Swagger'da görünen API Integration uçları için hafif istemci
    public sealed class IntegrationClient
    {
        private readonly HttpApiClient _api;
        private readonly SettingsStore _settingsStore;

        public IntegrationClient(HttpApiClient api, SettingsStore settingsStore)
        {
            _api = api;
            _settingsStore = settingsStore;
        }

        // PUT /integration/job/create
        // Multipart PUT /integration/job/create (X-API-Key destekli)
        public async Task<HttpResponseMessage> CreateJobsAsync(
            string apiKey,
            string batchName,
            string? passwordList,
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

            var response = await http.PutAsync("integration/job/create", form, ct);
            return response;
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

        // GET /integration/job/list
        public Task<HttpResponseMessage> ListJobsAsync(CancellationToken ct = default)
        {
            return _api.GetRawAsync("integration/job/list", ct);
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


