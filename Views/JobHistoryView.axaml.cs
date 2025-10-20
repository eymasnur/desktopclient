using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
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

        private Border CreateJobRow(string fileName, string status, long fileSize, string createdAt, string jobId)
        {
            var row = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#E5E7EB")),
                BorderThickness = new Avalonia.Thickness(0, 0, 0, 1),
                Padding = new Avalonia.Thickness(16, 0, 16, 0),
                Height = 42
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("3*,1.5*,1*,1.5*,0.5*")
            };

            // Column 0: File Name
            var fileNameText = new TextBlock
            {
                Text = fileName ?? "Unknown",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#1F2937")),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(fileNameText, 0);
            grid.Children.Add(fileNameText);

            // Column 1: Status Badge
            var statusBadge = CreateStatusBadge(status);
            Grid.SetColumn(statusBadge, 1);
            grid.Children.Add(statusBadge);

            // Column 2: File Size
            var sizeText = new TextBlock
            {
                Text = FormatFileSize(fileSize),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(sizeText, 2);
            grid.Children.Add(sizeText);

            // Column 3: Created At
            var dateText = new TextBlock
            {
                Text = FormatDate(createdAt),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dateText, 3);
            grid.Children.Add(dateText);

            // Column 4: Actions (Download + 3 Dots)
            var actionsPanel = CreateActionsPanel(status, jobId);
            Grid.SetColumn(actionsPanel, 4);
            grid.Children.Add(actionsPanel);

            row.Child = grid;
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
                CornerRadius = new Avalonia.CornerRadius(12),
                Padding = new Avalonia.Thickness(12, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = displayText,
                    FontSize = 12,
                    FontWeight = Avalonia.Media.FontWeight.Medium,
                    Foreground = new SolidColorBrush(Color.Parse(textColor))
                }
            };

            return badge;
        }

        private StackPanel CreateActionsPanel(string status, string jobId)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Download butonu (sadece SANITIZED için)
            if (status?.ToUpper() == "SANITIZED")
            {
                var downloadBtn = new Button
                {
                    Width = 20,
                    Height = 20,
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0),
                    Padding = new Avalonia.Thickness(0),
                    Content = new TextBlock
                    {
                        Text = "📥",
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                };
                downloadBtn.Click += (s, e) => DownloadFile(jobId);
                panel.Children.Add(downloadBtn);
            }

            // 3 nokta butonu (hep göster)
            var moreBtn = new Button
            {
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0),
                Content = new TextBlock
                {
                    Text = "⋮",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            moreBtn.Click += (s, e) => ShowMoreOptions(jobId);
            panel.Children.Add(moreBtn);

            return panel;
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024 * 1024)} MB";
            else
                return $"{bytes / (1024 * 1024 * 1024)} GB";
        }

        private string FormatDate(string dateString)
        {
            try
            {
                var date = DateTime.Parse(dateString);
                return date.ToString("dd-MM-yyyy HH:mm");
            }
            catch
            {
                return dateString;
            }
        }

        private void DownloadFile(string jobId)
        {
            Console.WriteLine($"📥 Download: {jobId}");
            // TODO: Download implementation
        }

        private void ShowMoreOptions(string jobId)
        {
            Console.WriteLine($"⋮ More options: {jobId}");
            // TODO: Context menu implementation
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