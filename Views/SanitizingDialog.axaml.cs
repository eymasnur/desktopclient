using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Desktop_client_api_kod.Views
{
    public partial class SanitizingDialog : Window
    {
        public SanitizingDialog()
        {
            InitializeComponent();
        }

        public void SetFileName(string fileName)
        {
            var fileNameText = this.FindControl<TextBlock>("FileNameText");
            if (fileNameText != null)
            {
                fileNameText.Text = fileName;
            }
        }

        public void SetStatus(string status)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                statusText.Text = status;
            }
        }
    }
}