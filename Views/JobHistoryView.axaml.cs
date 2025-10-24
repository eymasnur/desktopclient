using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Services;
using System.IO;
using System.Collections.Generic;

namespace Desktop_client_api_kod.Views
{
    public partial class JobHistoryView : UserControl
    {
        private readonly SettingsStore _settingsStore;
        private readonly IntegrationClient _integrationClient;

        public JobHistoryView()
        {
            InitializeComponent();
            
            _settingsStore = new SettingsStore();
            var httpClient = new HttpApiClient(_settingsStore);
            _integrationClient = new IntegrationClient(httpClient, _settingsStore);
            
            // ✅ Drag & Drop'u aktif et
            SetupDragAndDrop();
            
            // ✅ Popup'ı başlangıçta gizle
            Console.WriteLine("🔧 Constructor: Popup durumu kontrol ediliyor...");
            Console.WriteLine($"   UploadProgressPopup null mu? {UploadProgressPopup == null}");
            Console.WriteLine($"   IsVisible: {UploadProgressPopup?.IsVisible}");
            HideUploadPopup();
            
            // Job listesini yükle
            _ = LoadJobsAsync();
        }

        // ================================================================
        // PUBLIC METHOD - MainWindow'dan çağrılabilir
        // ================================================================
        
        /// <summary>
        /// Dışarıdan (MainWindow) dosya drop edildiğinde çağrılır
        /// Sadece TEK DOSYA kabul eder
        /// </summary>
        public async void HandleFilesDropped(System.Collections.Generic.List<string> filePaths)
        {
            Console.WriteLine($"📥 JobHistoryView.HandleFilesDropped çağrıldı: {filePaths.Count} dosya");
            
            // ✅ Tek dosya kontrolü
            if (filePaths.Count > 1)
            {
                Console.WriteLine($"⚠️ Çok fazla dosya ({filePaths.Count}), sadece 1 kabul edilir!");
                await ShowSingleFileWarningAsync();
                return;
            }
            
            await UploadFilesAsync(filePaths);
        }

        /// <summary>
        /// Context menu'den gelen dosyayı upload eder (popup göstermeden)
        /// </summary>
        public async Task UploadFileForContextMenuAsync(string filePath)
        {
            Console.WriteLine($"\n🎯 UploadFileForContextMenuAsync başladı: {Path.GetFileName(filePath)}");
            
            try
            {
                // 1. Dosya var mı kontrol et
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Dosya bulunamadı: {filePath}");
                    throw new FileNotFoundException($"File not found: {filePath}");
                }
                
                // 2. Settings'ten API Key al
                var settings = await _settingsStore.LoadAsync();
                
                var apiKey = settings.ApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    apiKey = "84e3ea0bc8fff1c93d1b5a42f3ac91432beb01b41a827001ff53a3832f227864";
                    Console.WriteLine("⚠️ Settings'te API Key yok, varsayılan kullanılıyor");
                }
                
                Console.WriteLine($"🔑 API Key: {apiKey.Substring(0, 20)}...");
                
                // 3. Batch name oluştur
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var batchName = $"context_menu_{timestamp}";
                
                Console.WriteLine($"📦 Batch Name: {batchName}");
                Console.WriteLine($"📤 API'ye gönderiliyor...");
                
                // 4. API'ye dosyayı yükle
                var response = await _integrationClient.CreateJobsAsync(
                    apiKey: apiKey,
                    batchName: batchName,
                    passwordList: null,
                    filePath: filePath,
                    allowInsecureCertificates: true
                );
                
                if (response != null && !response.error)
                {
                    Console.WriteLine($"✅ Upload başarılı!");
                    Console.WriteLine($"   Job ID: {response.data?.user_job_ids?[0]}");
                    Console.WriteLine($"   Batch ID: {response.data?.id}");
                }
                else
                {
                    Console.WriteLine($"❌ Upload başarısız: {response?.message}");
                    throw new Exception(response?.message ?? "Upload failed");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ========================================");
                Console.WriteLine($"❌ CONTEXT MENU UPLOAD HATASI");
                Console.WriteLine($"❌ Mesaj: {ex.Message}");
                Console.WriteLine($"❌ ========================================\n");
                throw;
            }
        }

        /// <summary>
        /// Job listesini dışarıdan yenilemek için public metod
        /// </summary>
        public async void RefreshJobList()
        {
            Console.WriteLine("🔄 Job listesi yenileniyor (external call)...");
            
            // Backend'in kaydetmesi için bekle
            await Task.Delay(3000);
            
            await LoadJobsAsync();
        }

        // ================================================================
        // DRAG & DROP SETUP
        // ================================================================
        
        private void SetupDragAndDrop()
        {
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, DragOver);
            AddHandler(DragDrop.DragLeaveEvent, DragLeave);
            AddHandler(DragDrop.DropEvent, Drop);
        }

        private void DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.GetFileNames() != null)
            {
                e.DragEffects = DragDropEffects.Copy;
                DragDropOverlay.IsVisible = true;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void DragLeave(object? sender, DragEventArgs e)
        {
            DragDropOverlay.IsVisible = false;
        }

        private async void Drop(object? sender, DragEventArgs e)
        {
            // Overlay'i gizle
            DragDropOverlay.IsVisible = false;
            e.Handled = true;
    
            var files = e.Data.GetFileNames()?.ToList();
            
            if (files == null || !files.Any())
            {
                return;
            }
            
            // ✅ TEK DOSYA KONTROLÜ
            if (files.Count > 1)
            {
                Console.WriteLine($"\n⚠️ ========================================");
                Console.WriteLine($"⚠️ ÇOK FAZLA DOSYA BIRAKILD!");
                Console.WriteLine($"⚠️ {files.Count} dosya seçildi, sadece 1 dosya yüklenebilir");
                Console.WriteLine($"⚠️ ========================================\n");
                
                // Kullanıcıya uyarı göster
                await ShowSingleFileWarningAsync();
                return;
            }
            
            var file = files[0];
            Console.WriteLine($"\n📁 Dosya bırakıldı: {Path.GetFileName(file)}");
            
            // ✅ Tek dosyayı upload et
            await UploadFilesAsync(new List<string> { file });
        }

        // ================================================================
        // UPLOAD LOGIC - ✅ TEK DOSYA UPLOAD
        // ================================================================
        
        private async Task UploadFilesAsync(List<string> filePaths)
        {
            // ✅ Güvenlik kontrolü - sadece tek dosya kabul et
            if (filePaths.Count != 1)
            {
                Console.WriteLine($"❌ Hata: {filePaths.Count} dosya geldi, sadece 1 dosya kabul edilir!");
                return;
            }
            
            var filePath = filePaths[0];
            bool hadError = false;
            string errorMessage = "";
            
            try
            {
                Console.WriteLine($"\n🚀 ========================================");
                Console.WriteLine($"🚀 UPLOAD İŞLEMİ BAŞLATILIYOR");
                Console.WriteLine($"🚀 Dosya: {Path.GetFileName(filePath)}");
                Console.WriteLine($"🚀 ========================================\n");
                
                // 1. Dosya var mı kontrol et
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Dosya bulunamadı: {filePath}");
                    return;
                }
                
                // 2. Settings'ten API Key al
                var settings = await _settingsStore.LoadAsync();
                
                var apiKey = settings.ApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    apiKey = "84e3ea0bc8fff1c93d1b5a42f3ac91432beb01b41a827001ff53a3832f227864";
                    Console.WriteLine("⚠️ Settings'te API Key yok, varsayılan kullanılıyor");
                }
                
                Console.WriteLine($"🔑 API Key: {apiKey.Substring(0, 20)}...");
                Console.WriteLine($"🌐 Base URL: {settings.BaseUrl}");
                
                // 3. Upload popup'ı göster
                Console.WriteLine("\n📱 ========================================");
                Console.WriteLine("📱 POPUP GÖSTERME İŞLEMİ");
                Console.WriteLine("📱 ========================================");
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Console.WriteLine("📱 UI Thread içindeyiz");
                    Console.WriteLine($"📱 UploadProgressPopup null mu? {UploadProgressPopup == null}");
                    Console.WriteLine("📱 ShowUploadPopup(1) çağrılıyor...");
                    ShowUploadPopup(1);
                    Console.WriteLine("📱 UpdateUploadFileList çağrılıyor...");
                    UpdateUploadFileList(filePaths);
                    Console.WriteLine("📱 Popup işlemleri tamamlandı");
                });
                
                Console.WriteLine("✅ Dispatcher'dan çıkıldı");
                
                // ✅ Popup'ın render olması için kısa delay
                await Task.Delay(200);
                Console.WriteLine("📱 ========================================\n");
                
                // 4. Dosyayı upload et
                Console.WriteLine($"───────────────────────────────────────");
                Console.WriteLine($"📂 Dosya: {Path.GetFileName(filePath)}");
                Console.WriteLine($"📏 Boyut: {FormatFileSize(new FileInfo(filePath).Length)}");
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateUploadStatus($"Uploading {Path.GetFileName(filePath)}...");
                });
                
                // Batch name oluştur
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var batchName = $"desktop_upload_{timestamp}";
                
                Console.WriteLine($"📦 Batch Name: {batchName}");
                Console.WriteLine($"📤 API'ye gönderiliyor...");
                
                // API'ye dosyayı yükle
                var response = await _integrationClient.CreateJobsAsync(
                    apiKey: apiKey,
                    batchName: batchName,
                    passwordList: null,
                    filePath: filePath,
                    allowInsecureCertificates: true
                );
                
                if (response != null && !response.error)
                {
                    Console.WriteLine($"✅ Upload başarılı!");
                    Console.WriteLine($"   Job ID: {response.data?.user_job_ids?[0]}");
                    Console.WriteLine($"   Batch ID: {response.data?.id}");
                    
                    // Success durumunu göster
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateUploadStatus("✓ Upload complete!");
                    });
                }
                else
                {
                    Console.WriteLine($"❌ Upload başarısız: {response?.message}");
                    hadError = true;
                    errorMessage = response?.message ?? "Unknown error";
                    
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ShowUploadError(errorMessage);
                    });
                }
                
                // 5. Kısa süre göster
                await Task.Delay(1500);
            }
            catch (Exception ex)
            {
                hadError = true;
                errorMessage = ex.Message;
                
                Console.WriteLine($"\n❌ ========================================");
                Console.WriteLine($"❌ UPLOAD İŞLEMİ HATASI");
                Console.WriteLine($"❌ Mesaj: {ex.Message}");
                Console.WriteLine($"❌ Stack: {ex.StackTrace}");
                Console.WriteLine($"❌ ========================================\n");
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowUploadError(ex.Message);
                });
                
                await Task.Delay(2000);
            }
            finally
            {
                // ✅ HER DURUMDA popup'ı kapat
                Console.WriteLine("🔄 ========================================");
                Console.WriteLine("🔄 POPUP KAPATILIYOR...");
                
                try
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        HideUploadPopup();
                    });
                    
                    Console.WriteLine("✅ Popup başarıyla kapatıldı");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Popup kapatma hatası: {ex.Message}");
                }
                
                // Job listesini yenile (başarılı upload varsa)
                if (!hadError)
                {
                    Console.WriteLine("\n🔄 JOB LİSTESİ YENİLENİYOR...");
                    Console.WriteLine("⏳ Backend'in job'ı kaydetmesi için 3 saniye bekleniyor...");
                    
                    try
                    {
                        await Task.Delay(3000);
                        
                        Console.WriteLine("📊 LoadJobsAsync çağrılıyor...");
                        await LoadJobsAsync();
                        Console.WriteLine("✅ Job listesi yenilendi");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Job listesi yenileme hatası: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Hata nedeniyle job listesi yenilenmedi: {errorMessage}");
                }
                
                Console.WriteLine("✅ UPLOAD İŞLEMİ TAMAMLANDI");
                Console.WriteLine("🔄 ========================================\n");
            }
        }
        
        // ================================================================
        // UPLOAD POPUP HELPERS
        // ================================================================
        
        private void ShowUploadPopup(int fileCount)
        {
            try
            {
                Console.WriteLine("📱 ShowUploadPopup çağrıldı");
                Console.WriteLine($"   Popup mevcut IsVisible: {UploadProgressPopup.IsVisible}");
                
                // ✅ Her zaman tekil: "Uploading file..."
                UploadTitleText.Text = "Uploading file...";
                
                // ✅ Popup'ı görünür yap
                UploadProgressPopup.IsVisible = true;
                
                Console.WriteLine($"   Popup yeni IsVisible: {UploadProgressPopup.IsVisible}");
                Console.WriteLine("✅ Popup görünür yapıldı");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ShowUploadPopup hatası: {ex.Message}");
            }
        }
        
        private void HideUploadPopup()
        {
            Console.WriteLine("🔄 HideUploadPopup çağrıldı");
            Console.WriteLine($"   Popup mevcut IsVisible: {UploadProgressPopup.IsVisible}");
            
            UploadProgressPopup.IsVisible = false;
            UploadFileListPanel.Children.Clear();
            
            Console.WriteLine($"   Popup yeni IsVisible: {UploadProgressPopup.IsVisible}");
            Console.WriteLine("✅ Popup gizlendi");
        }
        
        private void UpdateUploadFileList(List<string> filePaths)
        {
            UploadFileListPanel.Children.Clear();
            
            foreach (var filePath in filePaths)
            {
                var fileName = Path.GetFileName(filePath);
                var fileSize = new FileInfo(filePath).Length;
                
                var fileText = new TextBlock
                {
                    Text = $"{fileName} ({FormatFileSize(fileSize)})",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                
                UploadFileListPanel.Children.Add(fileText);
            }
        }
        
        private void UpdateUploadStatus(string status)
        {
            UploadStatusText.Text = status;
            // Rengi normal renge döndür (hata rengi kaldır)
            UploadStatusText.Foreground = new SolidColorBrush(Color.Parse("#6B7280"));
        }
        
        private void ShowUploadError(string error)
        {
            UploadStatusText.Text = $"Error: {error}";
            UploadStatusText.Foreground = new SolidColorBrush(Color.Parse("#DC2626"));
        }
        
        private void CloseUploadButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("❌ Kullanıcı popup'ı manuel olarak kapattı");
            HideUploadPopup();
        }

        /// <summary>
        /// Çoklu dosya bırakıldığında kullanıcıya uyarı gösterir
        /// </summary>
        private async Task ShowSingleFileWarningAsync()
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Popup'ı göster
                UploadProgressPopup.IsVisible = true;
                UploadTitleText.Text = "⚠️ Multiple Files Detected";
                
                // Dosya listesini temizle
                UploadFileListPanel.Children.Clear();
                
                // Uyarı mesajı ekle
                var warningText = new TextBlock
                {
                    Text = "Only one file can be uploaded at a time.\nPlease select a single file.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#DC2626")),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                };
                UploadFileListPanel.Children.Add(warningText);
                
                // Status güncelle
                UploadStatusText.Text = "Upload cancelled";
                UploadStatusText.Foreground = new SolidColorBrush(Color.Parse("#DC2626"));
            });
            
            // 3 saniye bekle
            await Task.Delay(3000);
            
            // Popup'ı kapat
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                HideUploadPopup();
            });
        }

        // ================================================================
        // JOB LOADING
        // ================================================================

        private async Task LoadJobsAsync()
        {
            try
            {
                Console.WriteLine("\n📊 ========================================");
                Console.WriteLine("📊 JOB LİSTESİ YÜKLENİYOR...");
                Console.WriteLine("📊 ========================================\n");
                
                var result = await _integrationClient.GetJobHistoryAsync();
                
                if (result == null)
                {
                    Console.WriteLine("❌ API'den null response geldi");
                    ToggleNoDataState(hasData: false);
                    return;
                }
                
                if (result.data == null || result.data.Count == 0)
                {
                    Console.WriteLine("❌ Hiç job bulunamadı (data boş)");
                    ToggleNoDataState(hasData: false);
                    return;
                }
                
                Console.WriteLine($"✅ {result.data.Count} job bulundu\n");
                
                ToggleNoDataState(hasData: true);
                
                // UI Thread'de job listesini güncelle
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Console.WriteLine("🔄 UI güncelleniyor...");
                    JobListPanel.Children.Clear();
                    
                    foreach (var item in result.data)
                    {
                        var jobInfo = item.user_job_info;
                        
                        Console.WriteLine($"   📄 {jobInfo.file_name} - {jobInfo.status}");
                        
                        var row = CreateJobRow(
                            jobInfo.file_name,
                            jobInfo.status,
                            jobInfo.file_size,
                            jobInfo.created_at,
                            jobInfo.user_job_id
                        );
                        
                        JobListPanel.Children.Add(row);
                    }
                });
                
                Console.WriteLine($"\n✅ {result.data.Count} dosya UI'da gösterildi!");
                Console.WriteLine("📊 ========================================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ========================================");
                Console.WriteLine($"❌ JOB LİSTESİ YÜKLEME HATASI");
                Console.WriteLine($"❌ Mesaj: {ex.Message}");
                Console.WriteLine($"❌ Stack: {ex.StackTrace}");
                Console.WriteLine($"❌ ========================================\n");
                
                ToggleNoDataState(hasData: false);
            }
        }

        // ================================================================
        // UI CREATION
        // ================================================================

        private Border CreateJobRow(string fileName, string status, long fileSize, 
                                   string createdAt, string jobId)
        {
            var row = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#E5E7EB")),
                BorderThickness = new Avalonia.Thickness(0, 0, 0, 1),
                Padding = new Avalonia.Thickness(24, 0, 24, 0),
                Height = 42,
                Margin = new Avalonia.Thickness(0),
                Background = Brushes.Transparent
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("654,159,107,168,70"),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 42,
                Margin = new Avalonia.Thickness(0)
            };

            // FILE NAME
            var fileNameText = new TextBlock
            {
                Text = fileName ?? "Unknown",
                FontSize = 13,
                FontWeight = Avalonia.Media.FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse("#1F2937")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0),
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(fileNameText, 0);
            grid.Children.Add(fileNameText);

            // STATUS
            var statusBadge = CreateStatusBadge(status);
            Grid.SetColumn(statusBadge, 1);
            grid.Children.Add(statusBadge);

            // SIZE
            var sizeText = new TextBlock
            {
                Text = FormatFileSize(fileSize),
                FontSize = 13,
                FontWeight = Avalonia.Media.FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0)
            };
            Grid.SetColumn(sizeText, 2);
            grid.Children.Add(sizeText);

            // CREATION TIME
            var dateText = new TextBlock
            {
                Text = FormatDate(createdAt),
                FontSize = 13,
                FontWeight = Avalonia.Media.FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0)
            };
            Grid.SetColumn(dateText, 3);
            grid.Children.Add(dateText);

            // ACTIONS
            var actionsPanel = CreateActionsPanel(status, jobId);
            Grid.SetColumn(actionsPanel, 4);
            grid.Children.Add(actionsPanel);

            row.Child = grid;
            
            // HOVER EFFECT
            row.PointerEntered += (s, e) =>
            {
                row.Background = new SolidColorBrush(Color.Parse("#F9FAFB"));
            };
            row.PointerExited += (s, e) =>
            {
                row.Background = Brushes.Transparent;
            };
            
            return row;
        }

        private Border CreateStatusBadge(string status)
        {
            string displayText;
            string backgroundColor;
            string textColor;

            switch (status?.ToUpper())
            {
                case "SANITIZED":
                    displayText = "Sanitized";
                    backgroundColor = "#D1FAE5";
                    textColor = "#059669";
                    break;
                    
                case "FAILED":
                    displayText = "Failed";
                    backgroundColor = "#FEE2E2";
                    textColor = "#DC2626";
                    break;
                    
                case "NOT_SANITIZABLE":
                case "NOT SANITIZABLE":
                    displayText = "Not Sanitizable";
                    backgroundColor = "#FEF3C7";
                    textColor = "#D97706";
                    break;
                    
                default:
                    displayText = status ?? "Unknown";
                    backgroundColor = "#F3F4F6";
                    textColor = "#6B7280";
                    break;
            }

            var badge = new Border
            {
                Background = new SolidColorBrush(Color.Parse(backgroundColor)),
                CornerRadius = new Avalonia.CornerRadius(12.5),
                Padding = new Avalonia.Thickness(8, 4, 8, 4),
                Height = 25,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = displayText,
                    FontSize = 13,
                    FontWeight = Avalonia.Media.FontWeight.Medium,
                    Foreground = new SolidColorBrush(Color.Parse(textColor)),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };

            return badge;
        }

        private StackPanel CreateActionsPanel(string status, string jobId)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Height = 42,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // DOWNLOAD BUTONU
            var downloadBtn = new Button
            {
                Width = 24,
                Height = 32,
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(4),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Content = new Image
                {
                    Source = new Bitmap("Assets/download-sanitized-file.png"),
                    Width = 16,
                    Height = 16,
                    Stretch = Avalonia.Media.Stretch.Uniform
                }
            };
            
            downloadBtn.Click += (s, e) => DownloadFile(jobId);
            
            downloadBtn.PointerEntered += (s, e) =>
            {
                downloadBtn.Background = new SolidColorBrush(Color.Parse("#F3F4F6"));
            };
            downloadBtn.PointerExited += (s, e) => 
            {
                downloadBtn.Background = Brushes.Transparent;
            };

            panel.Children.Add(downloadBtn);
            
            // 3 NOKTA BUTONU
            var moreBtn = new Button
            {
                Width = 32,
                Height = 32,
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Content = new Image
                {
                    Source = new Bitmap("Assets/Icon.png"),
                    Width = 16,
                    Height = 16,
                    Stretch = Avalonia.Media.Stretch.Uniform,
                }          
            };
            
            moreBtn.Click += (s, e) => ShowMoreOptions(jobId);
            
            moreBtn.PointerEntered += (s, e) => 
            {
                moreBtn.Opacity = 0.7;
            };
            moreBtn.PointerExited += (s, e) => 
            {
                moreBtn.Opacity = 1.0;
            };
            
            panel.Children.Add(moreBtn);

            return panel;
        }

        // ================================================================
        // HELPERS
        // ================================================================

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{Math.Round((double)bytes / 1024, 0)} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{Math.Round((double)bytes / (1024 * 1024), 1)} MB";
            else
                return $"{Math.Round((double)bytes / (1024 * 1024 * 1024), 2)} GB";
        }

        private string FormatDate(string dateString)
        {
            try
            {
                var date = DateTime.Parse(dateString);
                return date.ToString("yyyy-MM-dd HH:mm");
            }
            catch
            {
                return dateString;
            }
        }

        // ================================================================
        // BUTTON CLICK HANDLERS
        // ================================================================

        private void DownloadFile(string jobId)
        {
            Console.WriteLine($"📥 Download: {jobId}");
        }

        private void ShowMoreOptions(string jobId)
        {
            Console.WriteLine($"⋮ More options: {jobId}");
        }

        // ================================================================
        // ✅ SANİTİZE FILE BUTONU - DOSYA SEÇME DİALOG
        // ================================================================
        
        /// <summary>
        /// Sanitize File butonuna tıklandığında dosya seçme dialog'u açar
        /// ve seçilen dosyayı upload eder (TEK DOSYA)
        /// </summary>
        private async void SanitizeFileButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("\n🗂️ ========================================");
            Console.WriteLine("🗂️ SANİTİZE FILE BUTONUNA TIKLANDI");
            Console.WriteLine("🗂️ ========================================\n");
            
            try
            {
                // ✅ Dosya seçme dialog'unu oluştur (TEK DOSYA)
                var dialog = new OpenFileDialog
                {
                    AllowMultiple = false,  // ✅ Sadece tek dosya seçilebilir
                    Title = "Select a file to sanitize"
                };
                
                Console.WriteLine("📂 Dosya seçme penceresi açılıyor (tek dosya)...");
                
                // Window referansını al (dialog'u göstermek için gerekli)
                var window = this.VisualRoot as Window;
                if (window == null)
                {
                    Console.WriteLine("❌ Window bulunamadı, dialog açılamıyor");
                    return;
                }
                
                // Dialog'u göster ve kullanıcının seçmesini bekle
                var result = await dialog.ShowAsync(window);
                
                // Kullanıcı dosya seçmeden iptal ettiyse
                if (result == null || result.Length == 0)
                {
                    Console.WriteLine("❌ Dosya seçilmedi (kullanıcı iptal etti)");
                    return;
                }
                
                // Seçilen dosyayı logla
                var selectedFile = result[0];
                Console.WriteLine($"✅ Dosya seçildi: {Path.GetFileName(selectedFile)}");
                
                Console.WriteLine("\n🚀 Upload işlemi başlatılıyor...\n");
                
                // ✅ Tek dosyayı listeye koyup upload metodunu çağır
                await UploadFilesAsync(new List<string> { selectedFile });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ========================================");
                Console.WriteLine($"❌ DOSYA SEÇME HATASI");
                Console.WriteLine($"❌ Mesaj: {ex.Message}");
                Console.WriteLine($"❌ Stack: {ex.StackTrace}");
                Console.WriteLine($"❌ ========================================\n");
            }
        }

        private void SanitizedFilesButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("📋 Sanitized Files butonuna tıklandı");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("⚙️ Settings butonuna tıklandı");
        }

        private void ToggleNoDataState(bool hasData)
        {
            if (hasData)
            {
                NoDataPanel.IsVisible = false;
                TablePanel.IsVisible = true;
            }
            else
            {
                NoDataPanel.IsVisible = true;
                TablePanel.IsVisible = false;
            }
        }
    }
}