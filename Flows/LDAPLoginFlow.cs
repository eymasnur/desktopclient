// LDAP login akışı şimdilik devre dışı bırakıldı.
// Dosya korunuyor ancak tüm içerik yorum satırı olarak bırakıldı.
//
// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using Desktop_client_api_kod.Infrastructure;
// using Desktop_client_api_kod.Services;
//
// namespace Desktop_client_api_kod.Flows
// {
//     public static class LDAPLoginFlow
//     {
//         public static async Task RunAsync(SettingsStore settingsStore, CancellationToken ct = default)
//         {
//             var auth = new AuthService(new HttpApiClient(settingsStore), settingsStore);
//             
//             Console.Write("Kullanıcı adı: ");
//             var username = Console.ReadLine()?.Trim() ?? string.Empty;
//             
//             Console.Write("Şifre: ");
//             var password = ReadPassword();
//             
//             var loggedIn = await auth.LoginWithLdapAsync(username, password, ct);
//             Console.WriteLine(loggedIn ? "ldap login success" : "ldap login failed");
//         }
//
//         private static string ReadPassword()
//         {
//             var pass = string.Empty;
//             ConsoleKey key;
//             do
//             {
//                 var keyInfo = Console.ReadKey(intercept: true);
//                 key = keyInfo.Key;
//                 if (key == ConsoleKey.Backspace && pass.Length > 0)
//                 {
//                     pass = pass.Substring(0, pass.Length - 1);
//                 }
//                 else if (!char.IsControl(keyInfo.KeyChar))
//                 {
//                     pass += keyInfo.KeyChar;
//                 }
//             } while (key != ConsoleKey.Enter);
//             Console.WriteLine();
//             return pass;
//         }
//     }
// }