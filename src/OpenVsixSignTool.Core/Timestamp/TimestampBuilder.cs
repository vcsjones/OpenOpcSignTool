using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core.Timestamp
{
    internal static partial class TimestampBuilder
    {
        public static Task<(TimestampResult, byte[])> RequestTimestamp(Uri timestampUri, HashAlgorithmName timestampAlgorithm, TimestampNonce nonce, TimeSpan timeout, byte[] content)
        {
            var info = new HashAlgorithmInfo(timestampAlgorithm);
            byte[] digest;
            using (var hash = info.Create())
            {
                digest = hash.ComputeHash(content);
            }
            return SubmitTimestampRequest(timestampUri, info.Oid, nonce, timeout, digest);
        }
    }
}
