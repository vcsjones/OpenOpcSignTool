using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool
{
    /// <summary>
    /// A context for performing signing operations based on a certificate.
    /// </summary>
    public class SigningContext : IDisposable
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
        public SigningContext(X509Certificate2 certificate, HashAlgorithmName pkcsHashAlgorithmName, HashAlgorithmName fileDigestAlgorithmName)
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

    internal interface ISign : IDisposable
    {
        byte[] SignDigest(byte[] digest, HashAlgorithmName pkcsAlgorithm);
        bool VerifyDigest(byte[] digest, byte[] signature, HashAlgorithmName pkcsAlgorithm);
    }

    internal class ECDsaSign : ISign
    {
        public ECDsaCurve ECDsaCurve { get; }
        private readonly ECDsa _algorithm;

        public ECDsaSign(X509Certificate2 certificate)
        {
            var curveOid = OidParser.ReadFromBytes(certificate.PublicKey.EncodedParameters.RawData);
            switch (curveOid.Value)
            {
                case KnownOids.EccCurves.EcdsaP256:
                    ECDsaCurve = ECDsaCurve.p256;
                    break;
                case KnownOids.EccCurves.EcdsaP384:
                    ECDsaCurve = ECDsaCurve.p384;
                    break;
                case KnownOids.EccCurves.EcdsaP521:
                    ECDsaCurve = ECDsaCurve.p521;
                    break;
                default:
                    throw new NotSupportedException("The specified ECC curve is not supported.");
            }
            _algorithm = certificate.GetECDsaPrivateKey();
        }

        //ECDSA doesn't have the PKCS#1 / PSS hashing problem, so the hash is thrown away.
        public byte[] SignDigest(byte[] digest, HashAlgorithmName pkcsAlgorithm) => _algorithm.SignHash(digest);

        public bool VerifyDigest(byte[] digest, byte[] signature, HashAlgorithmName pkcsAlgorithm) => _algorithm.VerifyHash(digest, signature);

        public void Dispose()
        {
            _algorithm.Dispose();
        }
    }

    internal class RSAPkcsSign : ISign
    {
        public ECDsaCurve ECDsaCurve { get; }
        private readonly RSA _algorithm;

        public RSAPkcsSign(X509Certificate2 certificate)
        {
            _algorithm = certificate.GetRSAPrivateKey();
        }

        public byte[] SignDigest(byte[] digest, HashAlgorithmName pkcsAlgorithm) => _algorithm.SignHash(digest, pkcsAlgorithm, RSASignaturePadding.Pkcs1);

        public bool VerifyDigest(byte[] digest, byte[] signature, HashAlgorithmName pkcsAlgorithm) => _algorithm.VerifyHash(digest, signature, pkcsAlgorithm, RSASignaturePadding.Pkcs1);

        public void Dispose()
        {
            _algorithm.Dispose();
        }
    }


    public enum SigningAlgorithm
    {
        Unkonwn = 0,
        RSA,
        ECDSA
    }

    public enum ECDsaCurve
    {
        p256,
        p384,
        p521
    }
}