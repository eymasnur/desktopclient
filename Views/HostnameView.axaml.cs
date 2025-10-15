using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Services;

namespace Desktop_client_api_kod.Views
{
    public partial class HostnameView : UserControl
    {
        private readonly SettingsStore _settingsStore;
        private readonly HostnameService _hostnameService;

        public HostnameView()
        {
            InitializeComponent();
            
            _settingsStore = new SettingsStore();
            _hostnameService = new HostnameService(_settingsStore);
        }

        private async void SetHostnameButton_Click(object sender, RoutedEventArgs e)
        {
            var hostname = HostnameTextBox.Text?.Trim();
            
            // Hata mesajını gizle
            HideError();
            
            if (string.IsNullOrEmpty(hostname))
            {
                ShowError("Please enter hostname");
                return;
            }
            
            // Loading state
            if (sender is Button btn)
            {
                btn.IsEnabled = false;
                btn.Content = "Connecting...";
            }
            
            // Validate hostname with 7 second timeout
            var baseUrl = hostname.StartsWith("http") 
                ? hostname.TrimEnd('/') + "/api/v1"
                : "https://" + hostname.TrimEnd('/') + "/api/v1";
            
            bool isValid = false;
            
            try
            {
                // Validation task
                var validationTask = _hostnameService.ValidateHostnameAsync(
                    baseUrl, 
                    allowInsecureCertificates: true
                );
                
                // Timeout task (7 saniye)
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(7));
                
                // Hangisi önce biterse onu al
                var completedTask = await Task.WhenAny(validationTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // Timeout oldu (7 saniye geçti)
                    isValid = false;
                }
                else
                {
                    // Validation tamamlandı
                    isValid = await validationTask;
                }
            }
            catch (Exception)
            {
                // Hata oldu
                isValid = false;
            }
            
            // Reset button state
            if (sender is Button btn2)
            {
                btn2.IsEnabled = true;
                btn2.Content = "Set Hostname";
            }
            
            if (isValid)
            {
                await _hostnameService.SetHostnameAsync(baseUrl);
                ShowSuccess("Connected successfully!");
                
                await Task.Delay(1000);
                NavigateToLogin();
            }
            else
            {
                ShowError("Invalid hostname");
            }
        }

        private void ShowSuccess(string message) 
        {
            // İsterseniz success mesajı da ekleyebilirsiniz
        }

        private void ShowError(string message) 
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.IsVisible = true;
            
            // Input border'ını kırmızı (#ED5E5E) yap
            HostnameTextBox.BorderBrush = new SolidColorBrush(Color.Parse("#ED5E5E"));
        }

        private void HideError()
        {
            ErrorTextBlock.IsVisible = false;
            
            // Input border'ını normal renge döndür
            HostnameTextBox.BorderBrush = new SolidColorBrush(Color.Parse("#CED4DA"));
        }

        private void NavigateToLogin()
        {
            if (this.VisualRoot is Window mainWindow)
            {
                mainWindow.Content = new LoginView();
            }
        }
    }
}