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
            
            // Buton event'lerini bağla
            SanitizeFileButton.Click += SanitizeFileButton_Click;
            SanitizedFilesButton.Click += SanitizedFilesButton_Click;
            SettingsButton.Click += SettingsButton_Click;
            CloseUploadButton.Click += CloseUploadButton_Click;
            
            // ✅ Drag & Drop'u aktif et
            SetupDragAndDrop();
            
            // Job listesini yükle
            _ = LoadJobsAsync();
        }

        // ================================================================
        // PUBLIC METHOD - MainWindow'dan çağrılabilir
        // ================================================================
        
        /// <summary>
        /// Dışarıdan (MainWindow) dosya drop edildiğinde çağrılır
        /// </summary>
        public void HandleFilesDropped(System.Collections.Generic.List<string> filePaths)
        {
            Console.WriteLine($"📥 JobHistoryView.HandleFilesDropped çağrıldı: {filePaths.Count} dosya");
            _ = UploadFilesAsync(filePaths);
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
            
            Console.WriteLine($"\n📁 {files.Count} dosya bırakıldı:");
            foreach (var file in files)
            {
                Console.WriteLine($"   - {file}");
            }
            
            // ✅ Upload işlemini başlat
            await UploadFilesAsync(files);
        }

        // ================================================================
        // UPLOAD LOGIC
        // ================================================================
        
        private async Task UploadFilesAsync(List<string> filePaths)
        {
            try
            {
                // 1. Settings'ten API Key al
                var settings = await _settingsStore.LoadAsync();
                
                // API Key kontrolü ve varsayılan değer
                var apiKey = settings.ApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    apiKey = "84e3ea0bc8fff1c93d1b5a42f3ac91432beb01b41a827001ff53a3832f227864";
                    Console.WriteLine("⚠️ Settings'te API Key yok, varsayılan kullanılıyor");
                }
                
                Console.WriteLine($"🔑 API Key: {apiKey.Substring(0, 20)}...");
                
                // 2. Upload popup'ı göster
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowUploadPopup(filePaths.Count);
                    UpdateUploadFileList(filePaths);
                });
                
                // 3. Her dosyayı sırayla upload et
                int successCount = 0;
                
                foreach (var filePath in filePaths)
                {
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"⚠️ Dosya bulunamadı: {filePath}");
                        continue;
                    }
                    
                    try
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            UpdateUploadStatus($"Uploading {Path.GetFileName(filePath)}...");
                        });
                        
                        // Batch name oluştur
                        var batchName = $"desktop_upload_{DateTime.Now:yyyyMMdd_HHmmss}";
                        
                        Console.WriteLine($"\n📤 Yükleniyor: {Path.GetFileName(filePath)}");
                        
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
                            Console.WriteLine($"✅ Upload başarılı: {Path.GetFileName(filePath)}");
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine($"❌ Upload başarısız: {response?.message}");
                        }
                        
                        // Kısa bir delay (rate limiting için)
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Upload hatası: {ex.Message}");
                    }
                }
                
                // 4. Upload tamamlandı
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateUploadStatus($"Upload complete! ({successCount}/{filePaths.Count})");
                });
                
                // 5. 1.5 saniye bekle, sonra popup'ı kapat
                await Task.Delay(1500);
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    HideUploadPopup();
                });
                
                // 6. Job listesini yenile
                if (successCount > 0)
                {
                    Console.WriteLine("\n🔄 Job listesi yenileniyor...");
                    await Task.Delay(2000); // Backend'in job'ı oluşturması için bekle
                    await LoadJobsAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload işlemi hatası: {ex.Message}");
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowUploadError(ex.Message);
                });
                
                await Task.Delay(2000);
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    HideUploadPopup();
                });
            }
        }
        
        // ================================================================
        // UPLOAD POPUP HELPERS
        // ================================================================
        
        private void ShowUploadPopup(int fileCount)
        {
            var fileText = fileCount == 1 ? "1 file" : $"{fileCount} files";
            UploadTitleText.Text = $"Uploading {fileText}...";
            UploadProgressPopup.IsVisible = true;
        }
        
        private void HideUploadPopup()
        {
            UploadProgressPopup.IsVisible = false;
            UploadFileListPanel.Children.Clear();
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
        }
        
        private void ShowUploadError(string error)
        {
            UploadStatusText.Text = $"Error: {error}";
            UploadStatusText.Foreground = new SolidColorBrush(Color.Parse("#DC2626"));
        }
        
        private void CloseUploadButton_Click(object sender, RoutedEventArgs e)
        {
            HideUploadPopup();
        }

        // ================================================================
        // JOB LOADING
        // ================================================================

        private async Task LoadJobsAsync()
        {
            try
            {
                Console.WriteLine("\n📊 Job'lar yükleniyor...\n");
                
                var result = await _integrationClient.GetJobHistoryAsync();
                
                if (result == null || result.data == null || result.data.Count == 0)
                {
                    ToggleNoDataState(hasData: false);
                    Console.WriteLine("❌ Hiç job bulunamadı");
                    return;
                }
                
                ToggleNoDataState(hasData: true);
                
                JobListPanel.Children.Clear();
                
                foreach (var item in result.data)
                {
                    var jobInfo = item.user_job_info;
                    
                    var row = CreateJobRow(
                        jobInfo.file_name,
                        jobInfo.status,
                        jobInfo.file_size,
                        jobInfo.created_at,
                        jobInfo.user_job_id
                    );
                    
                    JobListPanel.Children.Add(row);
                }
                
                Console.WriteLine($"✅ {result.data.Count} dosya UI'da gösterildi!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HATA: {ex.Message}\n");
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

        private void SanitizeFileButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("🗂️ Sanitize File butonuna tıklandı");
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