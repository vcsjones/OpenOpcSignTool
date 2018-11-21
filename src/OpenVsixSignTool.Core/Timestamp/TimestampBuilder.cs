using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core.Timestamp
{
    internal static partial class TimestampBuilder
    {
        public static Task<(TimestampResult, byte[])> RequestTimestamp(Uri timestampUri, HashAlgorithmName timestampAlgorithm, TimestampNonce nonce, TimeSpan timeout, byte[] content)
        {
            var digestOid = HashAlgorithmTranslator.TranslateFromNameToOid(timestampAlgorithm);
            byte[] digest;
            using (var hash = HashAlgorithmTranslator.TranslateFromNameToXmlDSigUri(timestampAlgorithm, out _))
            {
                digest = hash.ComputeHash(content);
            }
            return SubmitTimestampRequest(timestampUri, digestOid, nonce, timeout, digest);
        }
    }
}
