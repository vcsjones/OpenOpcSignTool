using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool.Core
{
    internal static class OpcPartDigestProcessor
    {
        public static (byte[] digest, Uri identifier) Digest(OpcPart part, HashAlgorithmName algorithmName)
        {
            using (var hashAlgorithm = HashAlgorithmTranslator.TranslateFromNameToxmlDSigUri(algorithmName, out var identifier))
            {
                using (var partStream = part.Open())
                {
                    var digest = hashAlgorithm.ComputeHash(partStream);
                    return (digest, identifier);
                }
            }
        }
    }

}
