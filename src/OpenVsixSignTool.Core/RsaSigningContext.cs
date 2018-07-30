using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// A signing context used for signing packages with Azure Key Vault Keys.
    /// </summary>
    public class RsaSigningContext : ISigningContext
    {
        private readonly RSA _rsa;

        public RsaSigningContext(RsaSignConfigurationSet configuration)
        {
            ContextCreationTime = DateTimeOffset.Now;
            _rsa = configuration.Rsa;

            FileDigestAlgorithmName = configuration.FileDigestAlgorithm;
            PkcsDigestAlgorithmName = configuration.PkcsDigestAlgorithm;
            Certificate = configuration.SigningCertificate;
        }

        /// <summary>
        /// Gets the date and time that this context was created.
        /// </summary>
        public DateTimeOffset ContextCreationTime { get; }

        /// <summary>
        /// Gets the file digest algorithm.
        /// </summary>
        public HashAlgorithmName FileDigestAlgorithmName { get; }
        public HashAlgorithmName PkcsDigestAlgorithmName { get; }

        /// <summary>
        /// Gets the certificate and public key used to validate the signature.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Gets the signature algorithm. Currently, only <see cref="SigningAlgorithm.RSA"/> is supported.
        /// </summary>
        public SigningAlgorithm SignatureAlgorithm { get; } = SigningAlgorithm.RSA;

        public Uri XmlDSigIdentifier => SignatureAlgorithmTranslator.SignatureAlgorithmToXmlDSigUri(SignatureAlgorithm, PkcsDigestAlgorithmName);

        public Task<byte[]> SignDigestAsync(byte[] digest)
        {
            var signature = _rsa.SignHash(digest, PkcsDigestAlgorithmName, RSASignaturePadding.Pkcs1);

            return Task.FromResult(signature);
        }

        public Task<bool> VerifyDigestAsync(byte[] digest, byte[] signature)
        {
            using (var publicKey = Certificate.GetRSAPublicKey())
            {
                return Task.FromResult(publicKey.VerifyHash(digest, signature, PkcsDigestAlgorithmName, RSASignaturePadding.Pkcs1));
            }
        }

        public void Dispose()
        {
        }
    }
}
