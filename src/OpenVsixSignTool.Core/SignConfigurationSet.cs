using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool.Core
{
    public sealed class SignConfigurationSet
    {
        public HashAlgorithmName FileDigestAlgorithm { get; set; }
        public HashAlgorithmName PkcsDigestAlgorithm { get; set; }
        public AsymmetricAlgorithm SigningKey{ get; set; }
        public X509Certificate2 SigningCertificate { get; set; }
        
    }
}
