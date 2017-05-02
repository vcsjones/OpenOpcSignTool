using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool.Core
{
    internal class RSAPkcsCertificateSign : ICertificateSign
    {
        private readonly RSA _algorithm;

        public RSAPkcsCertificateSign(X509Certificate2 certificate) => _algorithm = certificate.GetRSAPrivateKey();

        public byte[] SignDigest(byte[] digest, HashAlgorithmName pkcsAlgorithm) =>
            _algorithm.SignHash(digest, pkcsAlgorithm, RSASignaturePadding.Pkcs1);

        public bool VerifyDigest(byte[] digest, byte[] signature, HashAlgorithmName pkcsAlgorithm) =>
            _algorithm.VerifyHash(digest, signature, pkcsAlgorithm, RSASignaturePadding.Pkcs1);

        public void Dispose() => _algorithm.Dispose();
    }
}