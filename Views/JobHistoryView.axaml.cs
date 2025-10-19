using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Desktop_client_api_kod.Views
{
    public partial class JobHistoryView : UserControl
    {
        public JobHistoryView()
        {
            InitializeComponent();
            
            // Event handler'ları bağla
            SanitizeFileButton.Click += SanitizeFileButton_Click;
            SanitizedFilesButton.Click += SanitizedFilesButton_Click;
            SettingsButton.Click += SettingsButton_Click;
            
            // Başlangıçta veri var mı kontrol et
            // ToggleNoDataState(hasData: true); // Mock veri var
        }

        private void SanitizeFileButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("🗂️ Sanitize File butonuna tıklandı");
            // TODO: File picker açılacak
        }

        private void SanitizedFilesButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("📋 Sanitized Files butonuna tıklandı");
            // TODO: Sadece sanitized dosyaları filtrele
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("⚙️ Settings butonuna tıklandı");
            // TODO: Settings ekranına geç
        }

        /// <summary>
        /// Veri var/yok durumunu toggle eder
        /// </summary>
        private void ToggleNoDataState(bool hasData)
        {
            // JobListPanel ve NoDataPanel'i toggle et
            if (hasData)
            {
                // Tablo göster, "No data" gizle
                NoDataPanel.IsVisible = false;
            }
            else
            {
                // Tablo gizle, "No data" göster
                NoDataPanel.IsVisible = true;
                // İsterseniz tablo panelini de gizleyebilirsiniz
            }
        }

        /// <summary>
        /// Mock veriyi yükler (test için)
        /// </summary>
        private void LoadMockData()
        {
            // TODO: API'den veri gelince burada yüklenecek
            // JobListPanel.Children.Clear();
            // foreach (var job in jobs) { ... }
        }
    }
}