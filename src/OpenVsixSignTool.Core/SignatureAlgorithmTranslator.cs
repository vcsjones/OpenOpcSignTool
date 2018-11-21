using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool.Core
{
    internal static class SignatureAlgorithmTranslator
    {
        public static Uri SignatureAlgorithmToXmlDSigUri(SigningAlgorithm signatureAlgorithm, HashAlgorithmName hashAlgorithmName)
        {
            switch (signatureAlgorithm)
            {
                case SigningAlgorithm.RSA when hashAlgorithmName.Name == HashAlgorithmName.MD5.Name:
                    return OpcKnownUris.SignatureAlgorithms.rsaMD5;
                case SigningAlgorithm.RSA when hashAlgorithmName.Name == HashAlgorithmName.SHA1.Name:
                    return OpcKnownUris.SignatureAlgorithms.rsaSHA1;
                case SigningAlgorithm.RSA when hashAlgorithmName.Name == HashAlgorithmName.SHA256.Name:
                    return OpcKnownUris.SignatureAlgorithms.rsaSHA256;
                case SigningAlgorithm.RSA when hashAlgorithmName.Name == HashAlgorithmName.SHA384.Name:
                    return OpcKnownUris.SignatureAlgorithms.rsaSHA384;
                case SigningAlgorithm.RSA when hashAlgorithmName.Name == HashAlgorithmName.SHA512.Name:
                    return OpcKnownUris.SignatureAlgorithms.rsaSHA512;

                case SigningAlgorithm.ECDSA when hashAlgorithmName.Name == HashAlgorithmName.SHA1.Name:
                    return OpcKnownUris.SignatureAlgorithms.ecdsaSHA1;
                case SigningAlgorithm.ECDSA when hashAlgorithmName.Name == HashAlgorithmName.SHA256.Name:
                    return OpcKnownUris.SignatureAlgorithms.ecdsaSHA256;
                case SigningAlgorithm.ECDSA when hashAlgorithmName.Name == HashAlgorithmName.SHA384.Name:
                    return OpcKnownUris.SignatureAlgorithms.ecdsaSHA384;
                case SigningAlgorithm.ECDSA when hashAlgorithmName.Name == HashAlgorithmName.SHA512.Name:
                    return OpcKnownUris.SignatureAlgorithms.ecdsaSHA512;
                default:
                    throw new NotSupportedException("The algorithm specified is not supported.");

            }
        }
    }
}
