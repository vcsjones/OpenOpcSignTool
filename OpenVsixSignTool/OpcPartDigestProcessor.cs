using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool
{
    internal class OpcPartDigestProcessor
    {
        public static (byte[] digest, Uri identifier) Digest(OpcPart part, HashAlgorithmName algorithmName)
        {
            using (var hashAlgorithm = HashAlgorithmTranslator.TranslateFromName(algorithmName, out var identifier))
            {
                var digest = hashAlgorithm.ComputeHash(part.Open());
                return (digest, identifier);
            }
        }
    }

}
