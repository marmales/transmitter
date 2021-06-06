using Microsoft.Extensions.Configuration;

namespace transmitter.Security
{
    public class CredentialsConfigurationSource : FileConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new CredentialsConfigProvider(this);
        }
    }
}