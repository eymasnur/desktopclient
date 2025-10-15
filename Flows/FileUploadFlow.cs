using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Services;

namespace Desktop_client_api_kod.Flows
{
    /// <summary>
    /// Sadece dosya yükleme işlemi (PUT)
    /// User Job ID döndürür
    /// </summary>
    public static class FileUploadFlow
    {
        public static async Task<string> RunAsync(
            SettingsStore settingsStore, 
            string apiKey,
            string filePath,
            CancellationToken ct = default)
        {
            Console.WriteLine($"\n📤 Dosya yükleniyor...");
            Console.WriteLine($"📁 Dosya: {Path.GetFileName(filePath)}");
            Console.WriteLine($"📏 Boyut: {new FileInfo(filePath).Length:N0} bytes");
            
            // Batch name oluştur
            var batchName = $"batch_{DateTime.Now:yyyyMMdd_HHmmss}";
            Console.WriteLine($"📦 Batch: {batchName}");
            
            try
            {
                // IntegrationClient oluştur
                var integration = new IntegrationClient(
                    new HttpApiClient(settingsStore), 
                    settingsStore
                );
                
                // PUT isteği - Dosyayı yükle
                var createResponse = await integration.CreateJobsAsync(
                    apiKey,
                    batchName,
                    null, // password_list (opsiyonel)
                    filePath,
                    allowInsecureCertificates: true,
                    ct
                );
                
                // Response kontrolü
                if (createResponse == null || createResponse.error)
                {
                    Console.WriteLine($"❌ Dosya yüklenemedi: {createResponse?.message}");
                    return null;
                }
                
                // User Job ID'yi al
                var jobIds = createResponse.data?.user_job_ids;
                if (jobIds == null || jobIds.Count == 0)
                {
                    Console.WriteLine("❌ Job ID alınamadı!");
                    return null;
                }
                
                var userJobId = jobIds[0];
                
                Console.WriteLine($"\n✅ Dosya başarıyla yüklendi!");
                Console.WriteLine($"📋 User Job ID: {userJobId}");
                Console.WriteLine($"📋 Batch ID: {createResponse.data?.id}");
                
                return userJobId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Yükleme hatası: {ex.Message}");
                return null;
            }
        }
    }
}