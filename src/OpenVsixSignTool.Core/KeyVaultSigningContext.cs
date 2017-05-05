using Microsoft.Azure.KeyVault;
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
        private readonly AzureKeyVaultMaterializedConfiguration _configuration;

        /// <summary>
        /// Creates a new siging context.
        /// </summary>
        public KeyVaultSigningContext(AzureKeyVaultMaterializedConfiguration configuration)
        {
            ContextCreationTime = DateTimeOffset.Now;
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the date and time that this context was created.
        /// </summary>
        public DateTimeOffset ContextCreationTime { get; }

        /// <summary>
        /// Gets the file digest algorithm.
        /// </summary>
        public HashAlgorithmName FileDigestAlgorithmName => _configuration.FileDigestAlgorithm;

        /// <summary>
        /// Gets the certificate and public key used to validate the signature.
        /// </summary>
        public X509Certificate2 Certificate => _configuration.PublicCertificate;

        /// <summary>
        /// Gets the signature algorithm. Currently, only <see cref="SigningAlgorithm.RSA"/> is supported.
        /// </summary>
        public SigningAlgorithm SignatureAlgorithm { get; } = SigningAlgorithm.RSA;

        public Uri XmlDSigIdentifier => SignatureAlgorithmTranslator.SignatureAlgorithmToXmlDSigUri(SignatureAlgorithm, _configuration.PkcsDigestAlgorithm);

        public async Task<byte[]> SignDigestAsync(byte[] digest)
        {
            var client = _configuration.Client;
            var signature = await client.SignAsync(_configuration.Key.KeyIdentifier.Identifier, "RS256", digest);
            return signature.Result;
        }

        public Task<bool> VerifyDigestAsync(byte[] digest, byte[] signature)
        {
            using (var publicKey = Certificate.GetRSAPublicKey())
            {
                return Task.FromResult(publicKey.VerifyHash(digest, signature, _configuration.PkcsDigestAlgorithm, RSASignaturePadding.Pkcs1));
            }
        }

        public void Dispose()
        {
        }
    }
}
