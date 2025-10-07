using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Models;
using System.Net;

namespace Desktop_client_api_kod.Services
{
    public sealed class HostnameService
    {
        private readonly SettingsStore _settingsStore;

        public HostnameService(SettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
        }

        public async Task<bool> ValidateHostnameAsync(string baseUrl, bool allowInsecureCertificates = true, string? apiKey = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) return false;

            try
            {
                // Scheme yoksa https varsay
                var normalized = baseUrl.Trim();
                if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    normalized = "https://" + normalized;
                }

                var handler = new HttpClientHandler();
                if (allowInsecureCertificates)
                {
                    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                using var http = new HttpClient(handler)
                {
                    BaseAddress = new Uri(normalized.TrimEnd('/') + "/"),
                    Timeout = TimeSpan.FromSeconds(5)
                };
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Klearis-Desktop-Client/1.0");
                http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, */*;q=0.1");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    http.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                }

                // Öncelik: /health varsa onu dene
                var pathsToTry = new[] { "api/v1/health", "health", "version", "", "/" };
                foreach (var path in pathsToTry)
                {
                    try
                    {
                        // Önce HEAD daha hızlıdır; desteklenmezse GET'e düş
                        HttpResponseMessage resp;
                        try
                        {
                            var request = new HttpRequestMessage(HttpMethod.Head, path);
                            resp = await http.SendAsync(request, ct);
                        }
                        catch
                        {
                            resp = await http.GetAsync(path, ct);
                        }
                        if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500)
                        {
                            return true; // 2xx veya 3xx ya da 401/403 gibi yanıtlar sunucunun ayakta olduğunu gösterir
                        }
                    }
                    catch
                    {
                        // sıradaki path'i dene
                    }
                }

                // https başarısızsa http ile de dene (geliştirme ağlarında faydalı)
                if (normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var httpUrl = "http://" + normalized.Substring("https://".Length);
                    using var httpFallback = new HttpClient(new HttpClientHandler())
                    {
                        BaseAddress = new Uri(httpUrl.TrimEnd('/') + "/"),
                        Timeout = TimeSpan.FromSeconds(5)
                    };
                    httpFallback.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Klearis-Desktop-Client/1.0");
                    httpFallback.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, */*;q=0.1");
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        httpFallback.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                    }
                    foreach (var path in pathsToTry)
                    {
                        try
                        {
                            HttpResponseMessage resp;
                            try
                            {
                                var request = new HttpRequestMessage(HttpMethod.Head, path);
                                resp = await httpFallback.SendAsync(request, ct);
                            }
                            catch
                            {
                                resp = await httpFallback.GetAsync(path, ct);
                            }
                            if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500)
                            {
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public async Task SetHostnameAsync(string baseUrl, CancellationToken ct = default)
        {
            var settings = await _settingsStore.LoadAsync();
            settings.BaseUrl = baseUrl.TrimEnd('/');
            await _settingsStore.SaveAsync(settings);
        }
    }
}


