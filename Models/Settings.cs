using System;

namespace Desktop_client_api_kod.Models
{
    public sealed class Settings
    {
        public string BaseUrl { get; set; } = "https://cdr12.klearis.cdr";
        public string AuthToken { get; set; } = string.Empty;
        public DateTimeOffset? AuthTokenExpiresAt { get; set; }
        public string ApiKey { get; set; } = string.Empty;
    }
}