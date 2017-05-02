using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// A context for performing signing operations with a certificate.
    /// </summary>
    public class CertificateSigningContext : ISigningContext
    {
        private readonly ISign _signProvider;
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
        public CertificateSigningContext(X509Certificate2 certificate, HashAlgorithmName pkcsHashAlgorithmName, HashAlgorithmName fileDigestAlgorithmName)
        {
            Certificate = certificate;
            ContextCreationTime = DateTimeOffset.Now;
            _pkcsHashAlgorithmName = pkcsHashAlgorithmName;
            FileDigestAlgorithmName = fileDigestAlgorithmName;
            switch (certificate.PublicKey.Oid.Value)
            {
                case KnownOids.X509Algorithms.RSA:
                    SignatureAlgorithm = SigningAlgorithm.RSA;
                    _signProvider = new RSAPkcsSign(certificate);
                    break;
                case KnownOids.X509Algorithms.Ecc:
                    SignatureAlgorithm = SigningAlgorithm.ECDSA;
                    _signProvider = new ECDsaSign(certificate);
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

        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Signs a digest.
        /// </summary>
        /// <param name="digest">The digest to sign.</param>
        /// <returns>A signature. The kind and size varies by algorithm and padding scheme.</returns>
        public byte[] SignDigest(byte[] digest) => _signProvider.SignDigest(digest, _pkcsHashAlgorithmName);

        /// <summary>
        /// Verifies a digest.
        /// </summary>
        /// <param name="digest">The digest to verify.</param>
        /// <param name="signature">The signature for the digest.</param>
        /// <returns>True if the signature is valid, otherwise false.</returns>
        public bool VerifyDigest(byte[] digest, byte[] signature) => _signProvider.VerifyDigest(digest, signature, _pkcsHashAlgorithmName);

        public Uri XmlDSigIdentifier
        {
            get
            {
                switch (SignatureAlgorithm)
                {
                    case SigningAlgorithm.RSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.MD5.Name:
                        return OpcKnownUris.SignatureAlgorithms.rsaMD5;
                    case SigningAlgorithm.RSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.SHA1.Name:
                        return OpcKnownUris.SignatureAlgorithms.rsaSHA1;
                    case SigningAlgorithm.RSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.SHA256.Name:
                        return OpcKnownUris.SignatureAlgorithms.rsaSHA256;
                    case SigningAlgorithm.RSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.SHA384.Name:
                        return OpcKnownUris.SignatureAlgorithms.rsaSHA384;
                    case SigningAlgorithm.RSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.SHA512.Name:
                        return OpcKnownUris.SignatureAlgorithms.rsaSHA512;

                    case SigningAlgorithm.ECDSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.SHA1.Name:
                        return OpcKnownUris.SignatureAlgorithms.ecdsaSHA1;
                    case SigningAlgorithm.ECDSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.SHA256.Name:
                        return OpcKnownUris.SignatureAlgorithms.ecdsaSHA256;
                    case SigningAlgorithm.ECDSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.SHA384.Name:
                        return OpcKnownUris.SignatureAlgorithms.ecdsaSHA384;
                    case SigningAlgorithm.ECDSA when _pkcsHashAlgorithmName.Name == HashAlgorithmName.SHA512.Name:
                        return OpcKnownUris.SignatureAlgorithms.ecdsaSHA512;
                    default:
                        throw new NotSupportedException("The algorithm specified is not supported.");
                }
            }
        }

        public void Dispose()
        {
            _signProvider.Dispose();
        }
    }
}