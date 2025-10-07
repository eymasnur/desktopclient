// KÜTÜPHANE İMPORT'LARI
// =====================
using System;                           // Temel C# fonksiyonları (Exception, Uri, Console vb.)
using System.Net.Http;                  // HTTP istekleri için (HttpClient, HttpResponseMessage)
using System.Net.Http.Headers;          // HTTP header'ları için (Authorization, Accept vb.)
using System.Text;                      // String encoding için (UTF8)
using System.Text.Json;                 // JSON işlemleri için (Serialize, Deserialize)
using System.Collections.Generic;       // Dictionary gibi koleksiyonlar için
using System.Threading;                 // CancellationToken için
using System.Threading.Tasks;           // Async/await için (Task, Task<T>)
using Desktop_client_api_kod.Models;    // Kendi yazdığımız Settings modeli için

// İSİM ALANI (NAMESPACE)
// ======================
// Bu sınıfı kategorize etmek için. Başka dosyalarda "using Desktop_client_api_kod.Infrastructure;" 
// yazarak bu sınıfı kullanabiliriz
namespace Desktop_client_api_kod.Infrastructure
{
    // SINIF TANIMI
    // ============
    // public = Herkes kullanabilir
    // sealed = Bu sınıftan başka sınıf türetilemez (inheritance yasak)
    // class = Sınıf tanımı
    public sealed class HttpApiClient
    {
        // ÜYE DEĞİŞKENLER (FIELDS)
        // ========================
        // private = Sadece bu sınıf içinden erişilebilir
        // readonly = Bir kez atandıktan sonra değiştirilemez (constructor'da atanır)
        
        // _httpClient = API'ye HTTP istekleri göndermek için kullanılır
        private readonly HttpClient _httpClient;
        
        // _settingsStore = Ayarları (BaseURL, Token, ApiKey) yüklemek/kaydetmek için kullanılır
        private readonly SettingsStore _settingsStore;

        // CONSTRUCTOR (YAPICI METOD)
        // ==========================
        // Bu metod "new HttpApiClient(...)" yazıldığında otomatik çalışır
        // Parametre: settingsStore = Ayarları yöneten nesne
        public HttpApiClient(SettingsStore settingsStore)
        {
            // Parametre olarak gelen settingsStore'u sınıf değişkenine ata
            // Böylece diğer metodlarda kullanabiliriz
            _settingsStore = settingsStore;
            
            // HttpClientHandler = HTTP isteklerinin nasıl yapılacağını kontrol eder
            var handler = new HttpClientHandler
            {
                // SSL sertifikası kontrolünü devre dışı bırak
                // NEDEN? https://192.168.16.161 gibi self-signed sertifikalara izin vermek için
                // ⚠️ UYARI: Production'da güvenlik riski! Sadece test/development ortamında kullan
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            
            // Yeni bir HttpClient oluştur ve yukarıdaki handler'ı kullan
            // Bu _httpClient tüm HTTP istekleri için kullanılacak
            _httpClient = new HttpClient(handler);
        }

        // CONFIGURE METODU
        // ================
        // Her HTTP isteği öncesi HttpClient'ı yapılandırır
        // BaseURL, Token, Headers gibi ayarları yapar
        // private = Sadece bu sınıf içinden çağrılabilir
        // async = Asenkron çalışır (await kullanabilir)
        // Task = İş yapar ama geriye değer döndürmez
        private async Task ConfigureAsync()
        {
            // Settings dosyasından ayarları yükle (BaseUrl, AuthToken, ApiKey)
            // await = Yükleme bitene kadar bekle
            var settings = await _settingsStore.LoadAsync();
            
            // BASE URL AYARLAMA
            // =================
            // BaseAddress = Tüm isteklerin başına eklenecek URL
            // Örnek: "https://192.168.16.161/api/v1/"
            // .TrimEnd('/') = Sondaki / varsa kaldır
            // + "/" = Sonuna tekrar / ekle (tutarlı olması için)
            _httpClient.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            
            // ACCEPT HEADER AYARLAMA
            // ======================
            // "Bana JSON formatında yanıt ver" header'ı
            _httpClient.DefaultRequestHeaders.Accept.Clear();  // Önceki Accept header'larını temizle
            _httpClient.DefaultRequestHeaders.Accept.Add(      // Yeni Accept header'ı ekle
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            // Sonuç: Accept: application/json

            // AUTHORIZATION HEADER AYARLAMA (BEARER TOKEN)
            // =============================================
            // Eğer AuthToken varsa Authorization header'ı ekle
            if (!string.IsNullOrWhiteSpace(settings.AuthToken))
            {
                // Bearer token ekle
                // Sonuç: Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", settings.AuthToken);
            }
            else
            {
                // Token yoksa Authorization header'ını kaldır
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }

            // X-API-KEY HEADER AYARLAMA
            // =========================
            const string apiKeyHeader = "X-API-Key";  // const = Değişmez sabit
            
            // Eğer önceden X-API-Key header'ı varsa kaldır (tekrar eklenmemesi için)
            if (_httpClient.DefaultRequestHeaders.Contains(apiKeyHeader))
            {
                _httpClient.DefaultRequestHeaders.Remove(apiKeyHeader);
            }
            
            // Eğer ApiKey ayarı varsa X-API-Key header'ını ekle
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add(apiKeyHeader, settings.ApiKey);
            }
            // Sonuç: X-API-Key: abc123def456
        }

        // GET JSON METODU
        // ===============
        // GET isteği gönderir ve JSON yanıtını C# nesnesine çevirir
        // public = Herkes kullanabilir
        // async = Asenkron çalışır
        // Task<T?> = T tipinde bir değer döndürür (nullable - null olabilir)
        // <T> = Generic tip (her tip için kullanılabilir: User, Product, SignInResponse vb.)
        // Parametreler:
        //   - path: Endpoint yolu (örn: "user/signin")
        //   - ct: İsteği iptal etmek için token (opsiyonel, varsayılan: default)
        public async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct = default)
        {
            // 1. Önce HttpClient'ı yapılandır (BaseURL, Token, Headers)
            await ConfigureAsync();
            
            // 2. GET isteği gönder
            // Tam URL: BaseAddress + path
            // Örnek: https://192.168.16.161/api/v1/ + user/profile = https://192.168.16.161/api/v1/user/profile
            var response = await _httpClient.GetAsync(path, ct);
            
            // 3. Yanıt başarılı mı kontrol et (200-299 arası status code)
            // Eğer hata varsa (400, 500 vb.) exception fırlat
            response.EnsureSuccessStatusCode();
            
            // 4. Yanıtın içeriğini stream olarak oku
            // Stream = Veriyi parça parça okumak için (bellek tasarrufu)
            var stream = await response.Content.ReadAsStreamAsync(ct);
            
            // 5. JSON string'ini C# nesnesine çevir (deserialize) ve döndür
            // <T> = Hangi tipe çevrileceği (GetJsonAsync<User>(...) → User nesnesi döner)
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
        }

        // POST JSON METODU
        // ================
        // POST isteği gönderir (JSON body ile), JSON yanıtını C# nesnesine çevirir
        // <TRequest, TResponse> = İki generic tip: İstek tipi ve Yanıt tipi
        // Parametreler:
        //   - path: Endpoint yolu (örn: "user/signin")
        //   - body: Gönderilecek veri (C# nesnesi, JSON'a çevrilecek)
        //   - ct: İptal token'ı (opsiyonel)
        public async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
            string path, 
            TRequest body, 
            CancellationToken ct = default)
        {
            // 1. HttpClient'ı yapılandır
            await ConfigureAsync();
            
            // 2. C# nesnesini JSON string'e çevir (serialize)
            // Örnek: {username:"admin", password:"Admin2022."} → JSON string
            var json = JsonSerializer.Serialize(body);
            
            // 3. DEBUG: Ne gönderildiğini konsola yazdır
            // $"..." = String interpolation (değişkenleri string içine koyma)
            Console.WriteLine($"POST URL: {_httpClient.BaseAddress}{path}");
            Console.WriteLine($"Request Body: {json}");
            
            // 4. JSON string'ini HTTP içeriğine çevir
            // using = İşimiz bitince otomatik temizle (memory leak önleme)
            // Encoding.UTF8 = Türkçe karakterler için UTF-8 kullan
            // "application/json" = Content-Type header'ı
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // 5. POST isteği gönder
            var response = await _httpClient.PostAsync(path, content, ct);
            
            // 6. DEBUG: Yanıtı konsola yazdır
            Console.WriteLine($"Response Status: {(int)response.StatusCode}");  // 200, 400, 500 vb.
            var responseBody = await response.Content.ReadAsStringAsync(ct);    // Yanıt içeriği
            Console.WriteLine($"Response Body: {responseBody}");
            
            // 7. Başarılı mı kontrol et, değilse hata fırlat
            response.EnsureSuccessStatusCode();
            
            // 8. JSON yanıtını C# nesnesine çevir ve döndür
            return JsonSerializer.Deserialize<TResponse>(responseBody);
        }

        // POST FORM METODU
        // ================
        // POST isteği gönderir ama JSON yerine FORM-ENCODED formatında
        // Form-encoded format: username=admin&password=Admin2022.&type=...
        // <TResponse> = Tek generic tip (sadece yanıt tipi)
        // Parametreler:
        //   - path: Endpoint yolu
        //   - formFields: Form alanları (Dictionary - anahtar-değer çiftleri)
        //   - ct: İptal token'ı
        public async Task<TResponse?> PostFormAsync<TResponse>(
            string path, 
            IDictionary<string, string> formFields,  // Dictionary<string, string>
            CancellationToken ct = default)
        {
            // 1. HttpClient'ı yapılandır
            await ConfigureAsync();
            
            // 2. DEBUG: Ne gönderildiğini konsola yazdır
            Console.WriteLine($"POST URL: {_httpClient.BaseAddress}{path}");
            Console.WriteLine($"Request Form: {System.Text.Json.JsonSerializer.Serialize(formFields)}");
            
            // 3. Dictionary'yi form-encoded formatına çevir
            // using = Otomatik temizlik
            // FormUrlEncodedContent = Content-Type: application/x-www-form-urlencoded
            // Format: key1=value1&key2=value2
            using var content = new FormUrlEncodedContent(formFields);
            
            // 4. POST isteği gönder
            var response = await _httpClient.PostAsync(path, content, ct);
            
            // 5. DEBUG: Yanıtı konsola yazdır
            Console.WriteLine($"Response Status: {(int)response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Response Body: {responseBody}");
            
            // 6. Başarılı mı kontrol et
            response.EnsureSuccessStatusCode();
            
            // 7. JSON yanıtını C# nesnesine çevir ve döndür
            return JsonSerializer.Deserialize<TResponse>(responseBody);
        }

        // GET RAW METODU
        // ==============
        // GET isteği gönderir ama yanıtı işlemez, HAM olarak döndürür
        // Neden kullanılır? Status code kontrolü, dosya indirme, özel durumlar için
        // HttpResponseMessage = Ham HTTP yanıtı (status code, headers, body hepsi içinde)
        public async Task<HttpResponseMessage> GetRawAsync(
            string path, 
            CancellationToken ct = default)
        {
            // 1. HttpClient'ı yapılandır
            await ConfigureAsync();
            
            // 2. GET isteği gönder ve yanıtı olduğu gibi döndür
            // JSON'a çevrilmez, HttpResponseMessage olarak döner
            return await _httpClient.GetAsync(path, ct);
        }
    }
}