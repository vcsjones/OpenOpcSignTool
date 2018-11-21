using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// A signing context used for signing packages with Azure Key Vault Keys.
    /// </summary>
    public class SigningContext : ISigningContext
    {
        private readonly SignConfigurationSet _configuration;

        /// <summary>
        /// Creates a new siging context.
        /// </summary>
        public SigningContext(SignConfigurationSet configuration)
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
        public X509Certificate2 Certificate => _configuration.SigningCertificate;

        /// <summary>
        /// Gets the signature algorithm.
        /// </summary>
        public SigningAlgorithm SignatureAlgorithm
        {
            get
            {
                switch (_configuration.SigningKey)
                {
                    case RSA _: return SigningAlgorithm.RSA;
                    case ECDsa _: return SigningAlgorithm.ECDSA;
                    default: return SigningAlgorithm.Unknown;
                }
            }
        }

        public Uri XmlDSigIdentifier => SignatureAlgorithmTranslator.SignatureAlgorithmToXmlDSigUri(SignatureAlgorithm, _configuration.PkcsDigestAlgorithm);

        public byte[] SignDigest(byte[] digest)
        {
            switch (_configuration.SigningKey)
            {
                case RSA rsa:
                    return rsa.SignHash(digest, _configuration.PkcsDigestAlgorithm, RSASignaturePadding.Pkcs1);
                case ECDsa ecdsa:
                    return ecdsa.SignHash(digest);
                default:
                    throw new InvalidOperationException("Unknown signing algorithm.");
            }
        }

        public bool VerifyDigest(byte[] digest, byte[] signature)
        {

            switch (SignatureAlgorithm)
            {
                case SigningAlgorithm.RSA:
                    using (var publicKey = Certificate.GetRSAPublicKey())
                    {
                        return publicKey.VerifyHash(digest, signature, _configuration.PkcsDigestAlgorithm, RSASignaturePadding.Pkcs1);
                    }
                case SigningAlgorithm.ECDSA:
                    using (var publicKey = Certificate.GetECDsaPublicKey())
                    {
                        return publicKey.VerifyHash(digest, signature);
                    }
                default:
                    throw new InvalidOperationException("Unknown signing algorithm.");
            }
        }

        public void Dispose()
        {
        }
    }
}
