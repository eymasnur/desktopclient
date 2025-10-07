using System;
using System.Threading;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Models;

namespace Desktop_client_api_kod.Services
{
    public sealed class AuthService
    {
        private readonly HttpApiClient _api;
        private readonly SettingsStore _settingsStore;

        public AuthService(HttpApiClient api, SettingsStore settingsStore)
        {
            _api = api;
            _settingsStore = settingsStore;
        }

        public sealed class SignInRequest
        {
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public string auth { get; set; } = string.Empty;
            public string os { get; set; } = string.Empty;
        }

        public sealed class SignInResponse
        {
            public TokensData tokens { get; set; } = new TokensData();
            public bool error { get; set; }
            public string? message { get; set; }
        }

        public sealed class TokensData
        {
            public string access { get; set; } = string.Empty;
            public string refresh { get; set; } = string.Empty;
        }

        public async Task<bool> LoginWithUserPassAsync(string username, string password, CancellationToken ct = default)
        {
            try
            {
                var request = new SignInRequest 
                { 
                    username = username,
                    password = password,
                    type = "91976df0-2bd2-472b-8c99-c06a07fe1b3c",
                    auth = "6709b914-dad0-468f-b713-1c370fa61716",  // ✅ DOĞRU UUID
                    os = "ed790d54-ed48-43b6-ab21-b93303305993"
                };

                var response = await _api.PostJsonAsync<SignInRequest, SignInResponse>("user/signin", request, ct);

                if (response == null || response.error || string.IsNullOrWhiteSpace(response.tokens.access))
                {
                    Console.WriteLine($"Login başarısız: {response?.message ?? "Bilinmeyen hata"}");
                    return false;
                }

                var settings = await _settingsStore.LoadAsync();
                settings.AuthToken = response.tokens.access;
                settings.AuthTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
                await _settingsStore.SaveAsync(settings);
                
                Console.WriteLine("Login başarılı!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login hatası: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateApiKeyAsync(CancellationToken ct = default)
        {
            try
            {
                var resp = await _api.GetRawAsync("integration/job/list", ct);
                var code = (int)resp.StatusCode;
                return code >= 200 && code < 404;
            }
            catch
            {
                return false;
            }
        }
    }
}