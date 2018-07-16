using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool.Core
{
    public interface ISignConfigurationSet
    {
        HashAlgorithmName FileDigestAlgorithm { get; }
        HashAlgorithmName PkcsDigestAlgorithm { get; }
        X509Certificate2 SigningCertificate { get; }
    }
}