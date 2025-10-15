
using System;
using Avalonia;

namespace Desktop_client_api_kod
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("🚀 Avalonia starting...");
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}