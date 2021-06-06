using Microsoft.Extensions.Configuration;

namespace transmitter.Security
{
    public static class SecurityConfigurationExtensions
    {
        public static IConfigurationBuilder AddCredentialSecrets(this IConfigurationBuilder builder, string path = "appsettings.secrets.json")
        {
            return builder.Add<CredentialsConfigurationSource>(options =>
            {
                options.Path = path;
            });
        }
    }
}