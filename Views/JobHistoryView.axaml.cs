using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;  // ✅ BUNU EKLE
using Desktop_client_api_kod.Infrastructure;

using Desktop_client_api_kod.Services;

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
            
            SanitizeFileButton.Click += SanitizeFileButton_Click;
            SanitizedFilesButton.Click += SanitizedFilesButton_Click;
            SettingsButton.Click += SettingsButton_Click;
            
            _ = LoadJobsAsync();
        }

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
                Spacing = 6, // İkonlar arası 6 px boşluk
                Height = 42, //satır yüksekliği

                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

           // DOWNLOAD BUTONU (Sadece SANITIZED için)
           var downloadBtn = new Button
                {
                    Width = 24, // 16px ikon 4px padding ile toplam 24px
                    Height = 32,
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0),// Border kaldırıldı
                    Padding = new Avalonia.Thickness(4), //her taraftan 4px padding
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    Content = new Image
                    {
                        Source = new Bitmap("Assets/download-sanitized-file.png"),
                        Width = 16,
                        Height = 16,
                        Stretch = Avalonia.Media.Stretch.Uniform
                    }
                };
                
                downloadBtn.Click += (s, e) => DownloadFile(jobId);// Download butonu başı
                
                downloadBtn.PointerEntered += (s, e) => // Hover efekti
                {
                    downloadBtn.Background = new SolidColorBrush(Color.Parse("#F3F4F6"));
                };
                downloadBtn.PointerExited += (s, e) => 
                {
                    downloadBtn.Background = Brushes.Transparent;
                };

                 panel.Children.Add(downloadBtn);// Download butonu sonu
            
                
            
            // 3 NOKTA BUTONU (Her zaman göster)
            var moreBtn = new Button
            {
                Width = 32,
                Height = 32,
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand), // El imlecine değiştir
                Content = new Image
                {
                    Source = new Bitmap("Assets/Icon.png"),
                    Width = 16,
                    Height = 16,
                    Stretch=Avalonia.Media.Stretch.Uniform,
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