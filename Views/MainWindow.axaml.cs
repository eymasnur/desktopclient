using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Desktop_client_api_kod.Views
{
    public partial class MainWindow : Window
    {
        private JobHistoryView? _jobHistoryView;
        private SanitizingDialog? _sanitizingDialog;
        private SuccessDialog? _successDialog;

        public MainWindow()
        {
            InitializeComponent();
            
            _jobHistoryView = this.FindControl<JobHistoryView>("JobHistoryView");
            
            Console.WriteLine("🏠 MainWindow initialized");
            
            // ✅ Command line'dan dosya geldiyse handle et
            _ = HandleStartupFileAsync();
        }

        /// <summary>
        /// Uygulama başlangıcında command line'dan dosya geldiyse işler
        /// </summary>
        private async Task HandleStartupFileAsync()
        {
            // UI'ın render olması için bekle
            await Task.Delay(500);
            
            var startupFile = Program.StartupFilePath;
            if (!string.IsNullOrEmpty(startupFile))
            {
                Console.WriteLine($"\n🎯 ========================================");
                Console.WriteLine($"🎯 CONTEXT MENU'DEN DOSYA GELDİ");
                Console.WriteLine($"🎯 Dosya: {startupFile}");
                Console.WriteLine($"🎯 ========================================\n");
                
                // Context menu flow başlat
                await HandleContextMenuUploadAsync(startupFile);
            }
        }

        /// <summary>
        /// Context menu'den gelen dosyayı işler
        /// Flow: Sanitizing Dialog → Upload → Success Dialog → Open App
        /// </summary>
        private async Task HandleContextMenuUploadAsync(string filePath)
        {
            try
            {
                // 1. Sanitizing Dialog'u göster
                await ShowSanitizingDialogAsync(filePath);
                
                // 2. Upload işlemini başlat
                bool success = await UploadFileAsync(filePath);
                
                // 3. Sanitizing Dialog'u kapat
                _sanitizingDialog?.Close();
                _sanitizingDialog = null;
                
                await Task.Delay(300);
                
                if (success)
                {
                    // 4. Success Dialog'u göster
                    await ShowSuccessDialogAsync(filePath);
                }
                else
                {
                    Console.WriteLine("❌ Upload başarısız, uygulama açılıyor...");
                    // Hata olsa bile uygulamayı göster
                    this.Show();
                    this.Activate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Context menu upload hatası: {ex.Message}");
                // Hata durumunda uygulamayı göster
                this.Show();
                this.Activate();
            }
        }

        /// <summary>
        /// "Sanitizing File..." dialog'unu gösterir
        /// </summary>
        private async Task ShowSanitizingDialogAsync(string filePath)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _sanitizingDialog = new SanitizingDialog();
                _sanitizingDialog.SetFileName(Path.GetFileName(filePath));
                
                Console.WriteLine("📱 Sanitizing Dialog gösteriliyor...");
                _sanitizingDialog.Show();
            });
        }

        /// <summary>
        /// Dosyayı API'ye upload eder
        /// </summary>
        private async Task<bool> UploadFileAsync(string filePath)
        {
            try
            {
                Console.WriteLine($"📤 Upload başlıyor: {filePath}");
                
                // JobHistoryView'deki upload metodunu kullan
                if (_jobHistoryView != null)
                {
                    await _jobHistoryView.UploadFileForContextMenuAsync(filePath);
                    Console.WriteLine("✅ Upload tamamlandı");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// "File Sanitized" success dialog'unu gösterir
        /// </summary>
        private async Task ShowSuccessDialogAsync(string filePath)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _successDialog = new SuccessDialog();
                _successDialog.SetMessage(Path.GetFileName(filePath));
                
                // "Open App" butonuna tıklanınca MainWindow'u göster
                _successDialog.OpenAppClicked += (s, e) =>
                {
                    Console.WriteLine("🏠 MainWindow açılıyor...");
                    this.Show();
                    this.Activate();
                    
                    // JobHistoryView'i yenile
                    _jobHistoryView?.RefreshJobList();
                };
                
                Console.WriteLine("✅ Success Dialog gösteriliyor...");
                _successDialog.Show();
            });
        }
    }
}