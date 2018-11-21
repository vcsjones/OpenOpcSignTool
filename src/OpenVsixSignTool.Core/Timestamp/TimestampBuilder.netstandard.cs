using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Tsp;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core.Timestamp
{
    static partial class TimestampBuilder
    {
        private static async Task<(TimestampResult, byte[])> SubmitTimestampRequest(Uri timestampUri, Oid digestOid, TimestampNonce nonce, TimeSpan timeout, byte[] digest)
        {
            var requestGenerator = new TimeStampRequestGenerator();
            var request = requestGenerator.Generate(digestOid.Value, digest, new Org.BouncyCastle.Math.BigInteger(nonce.Nonce.ToArray()));
            var encodedRequest = request.GetEncoded();
            var client = new HttpClient();
            client.Timeout = timeout;
            var content = new ByteArrayContent(encodedRequest);
            content.Headers.Add("Content-Type", "application/timestamp-query");
            try
            {
                var post = await client.PostAsync(timestampUri, content);
                if (post.StatusCode != HttpStatusCode.OK)
                {
                    return (TimestampResult.Failed, null);
                }
                var responseBytes = await post.Content.ReadAsByteArrayAsync();
                var responseParser = new Asn1StreamParser(responseBytes);
                var timeStampResponse = new TimeStampResponse(responseBytes);
                var tokenResponse = timeStampResponse.TimeStampToken.GetEncoded();
                return (TimestampResult.Success, tokenResponse);
            }
            catch
            {
                return (TimestampResult.Failed, null);
            }
        }
    }
}
