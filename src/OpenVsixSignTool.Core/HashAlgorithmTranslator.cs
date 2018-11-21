using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// Helper for translating a hash algorithm name to an instance of a hash algorithm and the
    /// URI XmlDSig identifier.
    /// </summary>
    internal static class HashAlgorithmTranslator
    {
        /// <summary>
        /// Translates the name of a hash algorithm.
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm to translate.</param>
        /// <param name="xmlDSigIdentifierUri">An XmlDSig URI that corresponds to the hash algorithm.</param>
        /// <returns>An instance of the hash algorithm.</returns>
        /// <remarks>The caller is expected to call <c>Dispose</c> on the return value.</remarks>
        public static HashAlgorithm TranslateFromNameToXmlDSigUri(HashAlgorithmName hashAlgorithmName, out Uri xmlDSigIdentifierUri)
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

        /// <summary>
        /// Translates the name of a hash algorithm.
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm to translate.</param>
        public static Oid TranslateFromNameToOid(HashAlgorithmName hashAlgorithmName)
        {
            if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                return new Oid(KnownOids.HashAlgorithms.md5);
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                return new Oid(KnownOids.HashAlgorithms.sha1);
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                return new Oid(KnownOids.HashAlgorithms.sha256);
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                return new Oid(KnownOids.HashAlgorithms.sha384);
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                return new Oid(KnownOids.HashAlgorithms.sha512);
            }
            else
            {
                throw new NotSupportedException("The algorithm selected is not supported.");
            }
        }
    }

}
