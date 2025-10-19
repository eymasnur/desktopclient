using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Services;

namespace Desktop_client_api_kod.Views
{
    public partial class LoginView : UserControl
    {
        private readonly SettingsStore _settingsStore;
        private readonly AuthService _authService;

        public LoginView()
        {
            InitializeComponent();
            
            // Services'leri oluştur
            _settingsStore = new SettingsStore();
            _authService = new AuthService(new HttpApiClient(_settingsStore), _settingsStore);
            
            // Login button event'ini bağla
            LoginButton.Click += LoginButton_Click;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordTextBox.Text;
            
            // Hata mesajını gizle
            HideError();
            
            // Boş kontrol
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter username and password");
                return;
            }
            
            // Loading state
            if (sender is Button btn)
            {
                btn.IsEnabled = false;
                btn.Content = "Logging in...";
            }
            
            bool loggedIn = false;
            
            try
            {
                // Login timeout (7 saniye)
                var loginTask = _authService.LoginWithUserPassAsync(username, password);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(7));
                
                var completedTask = await Task.WhenAny(loginTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // Timeout
                    loggedIn = false;
                }
                else
                {
                    // Login tamamlandı
                    loggedIn = await loginTask;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login exception: {ex.Message}");
                loggedIn = false;
            }
            
            // Reset button state
            if (sender is Button btn2)
            {
                btn2.IsEnabled = true;
                btn2.Content = "Login";
            }
            
            if (loggedIn)
            {
                // ✅ Başarılı login
                Console.WriteLine("✅ Login successful!");
                await Task.Delay(500);
                NavigateToNextView();
            }
            else
            {
                // ❌ Başarısız login
                ShowError("Invalid username or password");
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Foreground = new SolidColorBrush(Color.Parse("#ED5E5E")); // Kırmızı
            ErrorTextBlock.IsVisible = true;
        }

        private void HideError()
        {
            ErrorTextBlock.IsVisible = false;
            
            // Input border'larını normal renge döndür
            UsernameTextBox.BorderBrush = new SolidColorBrush(Color.Parse("#CED4DA"));
            PasswordTextBox.BorderBrush = new SolidColorBrush(Color.Parse("#CED4DA"));
        }

        private void NavigateToNextView()
        {
            // TODO: Sonraki ekrana yönlendir (örn: FileUploadView)
            if (this.VisualRoot is Window mainWindow)
            {
                // Geçici: Success mesajı göster
                ErrorTextBlock.Text = "Login successful!";
                ErrorTextBlock.Foreground = new SolidColorBrush(Color.Parse("#10B981")); // Yeşil
                ErrorTextBlock.IsVisible = true;
                mainWindow.Content = new JobHistoryView();  // ✅ Job History'ye geç
                
                // mainWindow.Content = new FileUploadView();
            }
        }
    }
}