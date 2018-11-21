using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// A configuration set for a signing operation.
    /// </summary>
    public sealed class SignConfigurationSet
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SignConfigurationSet"/>.
        /// </summary>
        /// <param name="fileDigestAlgorithm">The <see cref="HashAlgorithmName"/> used to digest files.</param>
        /// <param name="pkcsDigestAlgorithm">The <see cref="HashAlgorithmName"/> used in PKCS1 signatures.</param>
        /// <param name="signingKey">An <see cref="AsymmetricAlgorithm"/> with a private key that is used to perform signing operations.</param>
        /// <param name="publicCertificate">An <see cref="X509Certificate2"/> that contains the public key and certificate used to embed in the signature.</param>
        public SignConfigurationSet(HashAlgorithmName fileDigestAlgorithm, HashAlgorithmName pkcsDigestAlgorithm, AsymmetricAlgorithm signingKey, X509Certificate2 publicCertificate)
        {
            FileDigestAlgorithm = fileDigestAlgorithm;
            PkcsDigestAlgorithm = pkcsDigestAlgorithm;
            SigningKey = signingKey;
            PublicCertificate = publicCertificate;
        }

        /// <summary>
        /// The <see cref="HashAlgorithmName"/> used to digest files.
        /// </summary>
        public HashAlgorithmName FileDigestAlgorithm { get; }

        /// <summary>
        /// The <see cref="HashAlgorithmName"/> used in PKCS1 signatures.
        /// </summary>
        public HashAlgorithmName PkcsDigestAlgorithm { get; }

        /// <summary>
        /// An <see cref="AsymmetricAlgorithm"/> with a private key that is used to perform signing operations.
        /// </summary>
        public AsymmetricAlgorithm SigningKey { get; }

        /// <summary>
        /// An <see cref="X509Certificate2"/> that contains the public key and certificate used to embed in the signature.
        /// </summary>
        public X509Certificate2 PublicCertificate { get; }
        
    }
}
