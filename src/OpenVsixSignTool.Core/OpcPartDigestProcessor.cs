using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool.Core
{
    internal static class OpcPartDigestProcessor
    {
        public static (byte[] digest, Uri identifier) Digest(OpcPart part, HashAlgorithmName algorithmName)
        {
            var info = new HashAlgorithmInfo(algorithmName);
            using (var hashAlgorithm = info.Create())
            {
                using (var partStream = part.Open())
                {
                    var digest = hashAlgorithm.ComputeHash(partStream);
                    return (digest, info.XmlDSigIdentifier);
                }
            }
        }
    }
}
