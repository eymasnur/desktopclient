using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Services;

namespace Desktop_client_api_kod.Flows
{
    /// <summary>
    /// Tam sanitize akışı:
    /// 1. Kayıt yolu iste
    /// 2. Dosya yolu iste
    /// 3. API Key iste
    /// 4. Dosyayı yükle (PUT)
    /// 5. Sanitize bekle (Status Check)
    /// 6. Temiz dosyayı indir (GET)
    /// 7. Kaydet
    /// </summary>
    public static class FileSanitizeFlow
    {
        public static async Task RunAsync(SettingsStore settingsStore, CancellationToken ct = default)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════╗");
            Console.WriteLine("║     DOSYA SANİTİZE İŞLEMİ             ║");
            Console.WriteLine("╚═══════════════════════════════════════╝");
            
            // ========================================
            // ADIM 1: KAYIT YOLUNU AL
            // ========================================
            Console.WriteLine("\n[ADIM 1/7] Kayıt Yolu");
            Console.Write("Temizlenmiş dosyanın kaydedileceği klasör yolu: ");
            var saveFolderPath = Console.ReadLine()?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(saveFolderPath))
            {
                Console.WriteLine("❌ Kayıt yolu girilmedi!");
                return;
            }
            
            // Klasör yoksa oluştur
            if (!Directory.Exists(saveFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(saveFolderPath);
                    Console.WriteLine($"✅ Klasör oluşturuldu: {saveFolderPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Klasör oluşturulamadı: {ex.Message}");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"✅ Kayıt yolu: {saveFolderPath}");
            }
            
            // ========================================
            // ADIM 2: DOSYA YOLUNU AL
            // ========================================
            Console.WriteLine("\n[ADIM 2/7] Dosya Seçimi");
            Console.Write("Sanitize edilecek dosyanın tam yolu: ");
            var filePath = Console.ReadLine()?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("❌ Dosya yolu girilmedi!");
                return;
            }
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ Dosya bulunamadı: {filePath}");
                return;
            }
            
            Console.WriteLine($"✅ Seçilen dosya: {Path.GetFileName(filePath)}");
            Console.WriteLine($"📏 Dosya boyutu: {new FileInfo(filePath).Length:N0} bytes");
            
            // ========================================
            // ADIM 3: API KEY İSTE
            // ========================================
            Console.WriteLine("\n[ADIM 3/7] API Key");
            Console.Write("X-API-Key: ");
            var apiKey = Console.ReadLine()?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("❌ API Key gerekli!");
                return;
            }
            
            // ========================================
            // ADIM 4: DOSYAYI YÜKLE (PUT)
            // ========================================
            Console.WriteLine("\n[ADIM 4/7] Dosya Yükleme");
            var userJobId = await FileUploadFlow.RunAsync(settingsStore, apiKey, filePath, ct);
            
            if (string.IsNullOrEmpty(userJobId))
            {
                Console.WriteLine("❌ Dosya yüklenemedi, işlem iptal edildi!");
                return;
            }
            
            // ========================================
            // ADIM 5: SANİTİZE BEKLENİYOR
            // ========================================
            Console.WriteLine("\n[ADIM 5/7] Sanitize İşlemi");
            Console.WriteLine("⏳ Sanitize işlemi tamamlanması bekleniyor...");
            Console.Write("İlerleme: ");
            
            var integration = new IntegrationClient(
                new HttpApiClient(settingsStore), 
                settingsStore
            );
            
            var sanitized = await WaitForSanitization(integration, userJobId, ct);
            
            if (!sanitized)
            {
                Console.WriteLine("\n❌ Sanitize işlemi tamamlanamadı!");
                return;
            }
            
            // ========================================
            // ADIM 6: TEMİZ DOSYAYI İNDİR (GET)
            // ========================================
            Console.WriteLine("\n[ADIM 6/7] Dosya İndirme");
            Console.WriteLine("📥 Sanitize edilmiş dosya indiriliyor...");
            
            try
            {
                var downloadResponse = await integration.DownloadSanitizedAsync(userJobId, ct);
                
                if (!downloadResponse.IsSuccessStatusCode)
                {
                    var errorBody = await downloadResponse.Content.ReadAsStringAsync(ct);
                    Console.WriteLine($"❌ Dosya indirilemedi!");
                    Console.WriteLine($"Status: {downloadResponse.StatusCode}");
                    Console.WriteLine($"Error: {errorBody}");
                    return;
                }
                
                var fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync(ct);
                
                // ========================================
                // ADIM 7: DOSYAYI KAYDET
                // ========================================
                Console.WriteLine("\n[ADIM 7/7] Dosya Kaydetme");
                
                // Dosya adı oluştur
                var originalFileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                var sanitizedFileName = $"{originalFileName}_sanitized{extension}";
                var fullSavePath = Path.Combine(saveFolderPath, sanitizedFileName);
                
                await File.WriteAllBytesAsync(fullSavePath, fileBytes, ct);
                
                // ========================================
                // BAŞARILI!
                // ========================================
                Console.WriteLine("\n╔═══════════════════════════════════════╗");
                Console.WriteLine("║   ✅ SANİTİZE İŞLEMİ TAMAMLANDI!      ║");
                Console.WriteLine("╚═══════════════════════════════════════╝");
                Console.WriteLine($"📁 Dosya adı    : {sanitizedFileName}");
                Console.WriteLine($"📏 Dosya boyutu : {fileBytes.Length:N0} bytes");
                Console.WriteLine($"📂 Kayıt yeri   : {fullSavePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ İndirme/Kaydetme hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sanitize işlemi tamamlanana kadar bekler
        /// </summary>
        private static async Task<bool> WaitForSanitization(
            IntegrationClient integration, 
            string userJobId, 
            CancellationToken ct)
        {
            const int maxAttempts = 120; // 2 dakika
            const int delayMs = 1000; // 1 saniye
            
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var statusResponse = await integration.GetJobStatusAsync(userJobId, ct);
                    var statusBody = await statusResponse.Content.ReadAsStringAsync(ct);
                    
                    // SANITIZED mı?
                    if (statusBody.Contains("\"status\":\"SANITIZED\"") || 
                        statusBody.Contains("SANITIZED"))
                    {
                        Console.WriteLine("\n✅ Sanitize işlemi tamamlandı!");
                        return true;
                    }
                    
                    // Hata var mı?
                    if (statusBody.Contains("\"status\":\"FAILED\"") || 
                        statusBody.Contains("\"status\":\"ERROR\"") ||
                        statusBody.Contains("FAILED") ||
                        statusBody.Contains("ERROR"))
                    {
                        Console.WriteLine($"\n❌ Sanitize işlemi başarısız!");
                        Console.WriteLine($"Status: {statusBody}");
                        return false;
                    }
                    
                    // İlerleme göster
                    if (i % 5 == 0) // Her 5 saniyede bir
                    {
                        Console.Write($"[{i}s]");
                    }
                    else
                    {
                        Console.Write(".");
                    }
                    
                    await Task.Delay(delayMs, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n⚠️ Status kontrolü hatası: {ex.Message}");
                    await Task.Delay(delayMs, ct);
                }
            }
            
            Console.WriteLine("\n⚠️ Timeout: Sanitize işlemi çok uzun sürdü (2 dakika)");
            return false;
        }
    }
}