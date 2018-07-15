using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Crypto = System.Security.Cryptography;

namespace OpenVsixSignTool.Core
{
    public sealed class RsaSignConfigurationSet : ISignConfigurationSet
    {
        public Crypto.HashAlgorithmName FileDigestAlgorithm { get; set; }
        public Crypto.HashAlgorithmName PkcsDigestAlgorithm { get; set; }

        public RSA Rsa { get; set; }

        public X509Certificate2 SigningCertificate { get; set; }
        
    }
}
