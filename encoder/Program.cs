using System;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CommandLine;

namespace Encoder
{
    class Program
    {
        static void Main(string[] args)
        {
            Input input = default;
            Parser.Default.ParseArguments<Input>(args).WithParsed(i => input = i);
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var cert = store.Certificates.Find(X509FindType.FindByThumbprint, input.Thumbprint, true)[0];
            ConsoleKey keyInfo;
            do
            {
                Console.WriteLine("Provide value to encrypt:");
                var value = Console.ReadLine() ?? throw new ArgumentException("Only non empty values are allowed.");
                var cms = new EnvelopedCms(new ContentInfo(Encoding.UTF8.GetBytes(value)));
                cms.Encrypt(new CmsRecipient(cert));
                Console.WriteLine(Convert.ToBase64String(cms.Encode()));
                Console.WriteLine("Press enter to encrypt another value.");
                keyInfo = Console.ReadKey().Key;
            } while (keyInfo == ConsoleKey.Enter);
        }
    }
}