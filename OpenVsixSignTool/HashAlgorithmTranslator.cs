using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool
{
    public static class HashAlgorithmTranslator
    {
        public static HashAlgorithm TranslateFromName(HashAlgorithmName hashAlgorithmName, out Uri xmlDSigIdentifierUri)
        {
            if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                xmlDSigIdentifierUri = OpcKnownUris.HashAlgorithms.md5DigestUri;
                return MD5.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                xmlDSigIdentifierUri = OpcKnownUris.HashAlgorithms.sha1DigestUri;
                return SHA1.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                xmlDSigIdentifierUri = OpcKnownUris.HashAlgorithms.sha256DigestUri;
                return SHA256.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                xmlDSigIdentifierUri = OpcKnownUris.HashAlgorithms.sha384DigestUri;
                return SHA384.Create();
            }

            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                xmlDSigIdentifierUri = OpcKnownUris.HashAlgorithms.sha512DigestUri;
                return SHA512.Create();
            }
            else
            {
                throw new NotSupportedException("The algorithm selected is not supported.");
            }
        }
    }

}
