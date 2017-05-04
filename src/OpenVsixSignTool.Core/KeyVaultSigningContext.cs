using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// A signing context used for signing packages with Azure Key Vault Keys.
    /// </summary>
    public class KeyVaultSigningContext : ISigningContext
    {
        private readonly HashAlgorithmName _signatureDigestAlgorithm;

        /// <summary>
        /// Creates a new siging context.
        /// </summary>
        public KeyVaultSigningContext(HashAlgorithmName fileDigestAlgorithm, HashAlgorithmName signatureDigestAlgorithm)
        {
            ContextCreationTime = DateTimeOffset.Now;
            _signatureDigestAlgorithm = signatureDigestAlgorithm;
            FileDigestAlgorithmName = fileDigestAlgorithm;
        }

        /// <summary>
        /// Gets the date and time that this context was created.
        /// </summary>
        public DateTimeOffset ContextCreationTime { get; }

        /// <summary>
        /// Gets the file digest algorithm.
        /// </summary>
        public HashAlgorithmName FileDigestAlgorithmName { get; }

        /// <summary>
        /// Gets the certificate and public key used to validate the signature.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Gets the signature algorithm. Currently, only <see cref="SigningAlgorithm.RSA"/> is supported.
        /// </summary>
        public SigningAlgorithm SignatureAlgorithm { get; } = SigningAlgorithm.RSA;

        public Uri XmlDSigIdentifier => SignatureAlgorithmTranslator.SignatureAlgorithmToXmlDSigUri(SignatureAlgorithm, _signatureDigestAlgorithm);

        public Task<byte[]> SignDigestAsync(byte[] digest)
        {
            return Task.FromException<byte[]>(new NotImplementedException());
        }

        public Task<bool> VerifyDigestAsync(byte[] digest, byte[] signature)
        {
            return Task.FromException<bool>(new NotImplementedException());
        }

        public void Dispose()
        {
        }
    }
}
