using System;
using System.Threading.Tasks;
using Desktop_client_api_kod.Infrastructure;
using Desktop_client_api_kod.Services;
using System.Net;
using System.Text.RegularExpressions;

namespace Desktop_client_api_kod
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var settingsStore = new SettingsStore();
            var hostnameService = new HostnameService(settingsStore);

            string? baseUrl = null;
            
            if (args.Length > 0)
            {
                var hostArg = args[0].Trim();
                if (!hostArg.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = "https://" + hostArg.TrimEnd('/') + "/api/v1";
                }
                else
                {
                    baseUrl = hostArg.TrimEnd('/') + "/api/v1";
                }
            }
            else
            {
                Console.Write("Hostname giriniz (ör. cdr12.klearis.cdr veya 192.168.16.161): ");
                var hostInput = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(hostInput))
                {
                    Console.WriteLine("Geçerli bir hostname girilmedi.");
                    return;
                }
                
                if (!hostInput.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = "https://" + hostInput.TrimEnd('/') + "/api/v1";
                }
                else
                {
                    baseUrl = hostInput.TrimEnd('/') + "/api/v1";
                }
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                Console.WriteLine("Geçerli bir URL girilmedi.");
                return;
            }

            // Hostname formatını kontrol et (IP veya domain)
            if (!IsValidHostname(baseUrl))
            {
                Console.WriteLine("invalid hostname");
                return;
            }

            var ok = await hostnameService.ValidateHostnameAsync(baseUrl, allowInsecureCertificates: true);
            if (!ok)
            {
                Console.WriteLine("invalid hostname");
                return;
            }

            await hostnameService.SetHostnameAsync(baseUrl);
            Console.WriteLine("valid hostname");

            // Username/Password login
            var auth = new AuthService(new HttpApiClient(settingsStore), settingsStore);
            Console.Write("Kullanıcı adı: ");
            var username = Console.ReadLine()?.Trim() ?? string.Empty;
            Console.Write("Şifre: ");
            var password = ReadPassword();
            var loggedIn = await auth.LoginWithUserPassAsync(username, password);
            Console.WriteLine(loggedIn ? "login success" : "login failed");
        }

        private static bool IsValidHostname(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname)) return false;

            // http:// veya https:// kısmını çıkar
            var normalized = hostname.Replace("https://", "").Replace("http://", "");
            
            // Port varsa ayır
            var host = normalized.Split(':')[0].Split('/')[0];

            // IP adresi kontrolü (IPv4)
            if (IPAddress.TryParse(host, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return true;
            }

            // Domain adı kontrolü
            var domainPattern = @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$";
            return Regex.IsMatch(host, domainPattern);
        }

        private static string ReadPassword()
        {
            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass.Substring(0, pass.Length - 1);
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass;
        }
    }
}