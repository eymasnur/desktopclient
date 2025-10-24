using System;
using System.Linq;
using Avalonia;

namespace Desktop_client_api_kod
{
    internal static class Program
    {
        // ✅ Command line'dan gelen dosya yolu
        public static string? StartupFilePath { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("🚀 Avalonia starting...");
            
            // ✅ Command line argument kontrolü
            if (args != null && args.Length > 0)
            {
                var filePath = args[0];
                Console.WriteLine($"📁 Command line argument: {filePath}");
                
                // Dosya var mı kontrol et
                if (System.IO.File.Exists(filePath))
                {
                    StartupFilePath = filePath;
                    Console.WriteLine($"✅ Startup file set: {filePath}");
                }
                else
                {
                    Console.WriteLine($"⚠️ File not found: {filePath}");
                }
            }
            
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