using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace Desktop_client_api_kod.Views
{
    public partial class SuccessDialog : Window
    {
        public event EventHandler? OpenAppClicked;

        public SuccessDialog()
        {
            InitializeComponent();
        }

        public void SetMessage(string fileName)
        {
            var messageText = this.FindControl<TextBlock>("MessageText");
            if (messageText != null)
            {
                messageText.Text = $"{fileName} has been sanitized. You can view it in the app.";
            }
        }

        private void OpenAppButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("✅ 'Open App' butonuna tıklandı");
            
            // Event'i tetikle (MainWindow dinleyecek)
            OpenAppClicked?.Invoke(this, EventArgs.Empty);
            
            // Dialog'u kapat
            this.Close();
        }
    }
}