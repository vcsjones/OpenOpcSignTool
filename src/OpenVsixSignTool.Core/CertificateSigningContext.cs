using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// A context for performing signing operations with a certificate.
    /// </summary>
    public class CertificateSigningContext : ISigningContext
    {
        private readonly ICertificateSign _signProvider;
        private readonly HashAlgorithmName _pkcsHashAlgorithmName;

        public SigningAlgorithm SignatureAlgorithm { get; }

        /// <summary>
        /// Creates a new signing context.
        /// </summary>
        /// <param name="certificate">The certificate for signing and verifying data.</param>
        /// <param name="pkcsHashAlgorithmName">
        /// A hash algorithm. Currently, this is used in the PKCS#1 padding operation with RSA. The value is ignored for
        /// ECC signatures. This should usually match the algorithm used to hash the data that will be signed and verified.
        /// </param>
        /// <param name="fileDigestAlgorithmName">
        /// A hash algorithm. This is the digest algorting used for digesting files.
        /// </param>
        public CertificateSigningContext(X509Certificate2 certificate, HashAlgorithmName pkcsHashAlgorithmName, HashAlgorithmName fileDigestAlgorithmName)
        {
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            ContextCreationTime = DateTimeOffset.Now;
            _pkcsHashAlgorithmName = pkcsHashAlgorithmName;
            FileDigestAlgorithmName = fileDigestAlgorithmName;
            switch (certificate.PublicKey.Oid.Value)
            {
                case KnownOids.X509Algorithms.RSA:
                    SignatureAlgorithm = SigningAlgorithm.RSA;
                    _signProvider = new RSAPkcsCertificateSign(certificate);
                    break;
                case KnownOids.X509Algorithms.Ecc:
                    SignatureAlgorithm = SigningAlgorithm.ECDSA;
                    _signProvider = new ECDsaCertificateSign(certificate);
                    break;
                default:
                    throw new NotSupportedException("The specified signature algorithm is not supported.");
            }
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
        /// Gets the certificate used to sign the package.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Signs a digest.
        /// </summary>
        /// <param name="digest">The digest to sign.</param>
        /// <returns>A signature. The kind and size varies by algorithm and padding scheme.</returns>
        public Task<byte[]> SignDigestAsync(byte[] digest) => Task.FromResult(_signProvider.SignDigest(digest, _pkcsHashAlgorithmName));

        /// <summary>
        /// Verifies a digest.
        /// </summary>
        /// <param name="digest">The digest to verify.</param>
        /// <param name="signature">The signature for the digest.</param>
        /// <returns>True if the signature is valid, otherwise false.</returns>
        public Task<bool> VerifyDigestAsync(byte[] digest, byte[] signature) => Task.FromResult(_signProvider.VerifyDigest(digest, signature, _pkcsHashAlgorithmName));

        public Uri XmlDSigIdentifier => SignatureAlgorithmTranslator.SignatureAlgorithmToXmlDSigUri(SignatureAlgorithm, _pkcsHashAlgorithmName);

        public void Dispose()
        {
            _signProvider.Dispose();
        }
    }
}