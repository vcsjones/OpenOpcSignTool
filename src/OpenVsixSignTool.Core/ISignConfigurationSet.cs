using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool.Core
{

    public sealed class CertificateSignConfigurationSet
    {
        public X509Certificate2 SigningCertificate { get; set; }
        public HashAlgorithmName FileDigestAlgorithm { get; set; }
        public HashAlgorithmName PkcsDigestAlgorithm { get; set; }

        public bool Validate()
        {
            // Logging candidate.
            if (SigningCertificate?.HasPrivateKey != true)
            {
                return false;
            }
            return true;
        }
    }

    public sealed class AzureKeyVaultSignConfigurationSet
    {
        public string AzureClientId { get; set; }
        public string AzureClientSecret { get; set; }
        public string AzureKeyVaultUrl { get; set; }
        public string AzureKeyVaultCertificateName { get; set; }

        public bool Validate()
        {
            // Logging candidate.
            if (string.IsNullOrWhiteSpace(AzureClientId))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(AzureClientSecret))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(AzureKeyVaultUrl))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(AzureKeyVaultCertificateName))
            {
                return false;
            }
            return true;
        }
    }
}
