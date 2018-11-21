using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace OpenVsixSignTool
{
    internal class KeyVaultConfigurationDiscoverer
    {
        public async Task<ErrorOr<AzureKeyVaultMaterializedConfiguration>> Materialize(AzureKeyVaultSignConfigurationSet configuration)
        {
            async Task<string> Authenticate(string authority, string resource, string scope)
            {
                if (!string.IsNullOrWhiteSpace(configuration.AzureAccessToken))
                {
                    return configuration.AzureAccessToken;
                }

                var context = new AuthenticationContext(authority);
                ClientCredential credential = new ClientCredential(configuration.AzureClientId, configuration.AzureClientSecret);

                try
                {
                    var result = await context.AcquireTokenAsync(resource, credential);
                    return result.AccessToken;
                }
                catch (AdalServiceException e) when (e.StatusCode >= 400 && e.StatusCode < 500)
                {
                    return null;
                }
            }

            var vault = new KeyVaultClient(Authenticate);
            var azureCertificate = await vault.GetCertificateAsync(configuration.AzureKeyVaultUrl, configuration.AzureKeyVaultCertificateName);
                
            var certificate = new X509Certificate2(azureCertificate.Cer);
            var keyId = azureCertificate.KeyIdentifier;
            return new AzureKeyVaultMaterializedConfiguration(vault, certificate, keyId);

        }
    }
}
