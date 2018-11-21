using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core.Timestamp
{
    static partial class TimestampBuilder
    {
        private static async Task<(TimestampResult, byte[])> SubmitTimestampRequest(Uri timestampUri, Oid digestOid, TimestampNonce nonce, TimeSpan timeout, byte[] digest)
        {
            var timestampRequest = Rfc3161TimestampRequest.CreateFromHash(digest, digestOid, nonce: nonce.Nonce, requestSignerCertificates: true);
            var encodedRequest = timestampRequest.Encode();
            var client = new HttpClient();
            var content = new ByteArrayContent(encodedRequest);
            content.Headers.Add("Content-Type", "application/timestamp-query");
            var post = await client.PostAsync(timestampUri, content);
            if (post.StatusCode != HttpStatusCode.OK)
            {
                return (TimestampResult.Failed, null);
            }
            var responseBytes = await post.Content.ReadAsByteArrayAsync();
            var token = timestampRequest.ProcessResponse(responseBytes, out _);
            var tokenInfo = token.AsSignedCms().Encode();
            return (TimestampResult.Success, tokenInfo);
        }
    }
}
