using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool.Core
{
    internal class OpcPartDigestProcessor
    {
        public static (byte[] digest, Uri identifier) Digest(OpcPart part, HashAlgorithmName algorithmName)
        {
            using (var hashAlgorithm = HashAlgorithmTranslator.TranslateFromNameToxmlDSigUri(algorithmName, out var identifier))
            {
                var digest = hashAlgorithm.ComputeHash(part.Open());
                return (digest, identifier);
            }
        }
    }

}
