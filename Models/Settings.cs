using System;

namespace Desktop_client_api_kod.Models
{
    public sealed class Settings
    {
        // ✅ DÜZELTME: BaseUrl'yi IP adresiyle güncelle
        public string BaseUrl { get; set; } = "https://192.168.16.161/api/v1";
        
        public string AuthToken { get; set; } = string.Empty;
        
        public DateTimeOffset? AuthTokenExpiresAt { get; set; }
        
        // ✅ DÜZELTME: API Key varsayılan değer ile
        public string ApiKey { get; set; } = "84e3ea0bc8fff1c93d1b5a42f3ac91432beb01b41a827001ff53a3832f227864";
    }
}