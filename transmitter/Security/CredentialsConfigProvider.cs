using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace transmitter.Security
{
    public class CredentialsConfigProvider : FileConfigurationProvider
    {
        private const string Thumbprint = "Thumbprint";
        public CredentialsConfigProvider(FileConfigurationSource source) : base(source)
        {
        }

        public override void Load(Stream stream)
        {
            try
            {
                var result = new Dictionary<string, string>();
                var dictionary = JsonConfigurationFileParser.Parse(stream);
                result.Add(Thumbprint, dictionary[Thumbprint]);
                var certificate = Certificate(result[Thumbprint]);
                foreach (var (key, value) in dictionary.Where(x => x.Key != Thumbprint))
                {
                    var decrypted = Decrypt(certificate, value);
                    result[key] = decrypted;
                }

                Data = result;
            }
            catch (JsonException e)
            {
                throw new FormatException("Error_JSONParseError", e);
            }
        }
        
        private static X509Certificate2Collection Certificate(string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, true);
        }
        private static string Decrypt(X509Certificate2Collection cert, string encrypted)
        {
            var cms = new EnvelopedCms();
            cms.Decode(Convert.FromBase64String(encrypted));
            cms.Decrypt(cert);
            return Encoding.UTF8.GetString(cms.ContentInfo.Content); 
        }
    }
}