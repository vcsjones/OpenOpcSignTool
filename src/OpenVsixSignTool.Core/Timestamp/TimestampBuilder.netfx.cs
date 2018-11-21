using OpenVsixSignTool.Core.Interop;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core.Timestamp
{
    static partial class TimestampBuilder
    {
        private static unsafe Task<(TimestampResult, byte[])> SubmitTimestampRequest(Uri timestampUri, Oid digestOid, TimestampNonce nonce, TimeSpan timeout, byte[] digest)
        {
            var parameters = new CRYPT_TIMESTAMP_PARA
            {
                cExtension = 0,
                fRequestCerts = true
            };
            using (var cNonce = nonce.Nonce.Pin())
            {
                parameters.Nonce.cbData = (uint)nonce.Nonce.Length;
                parameters.Nonce.pbData = new IntPtr(cNonce.Pointer);
                parameters.pszTSAPolicyId = null;
                var winResult = Crypt32.CryptRetrieveTimeStamp(
                    timestampUri.AbsoluteUri,
                    CryptRetrieveTimeStampRetrievalFlags.NONE,
                    (uint)timeout.TotalMilliseconds,
                    digestOid.Value,
                    ref parameters,
                    digest,
                    (uint)digest.Length,
                    out var context,
                    IntPtr.Zero,
                    IntPtr.Zero
                );
                if (!winResult)
                {
                    return Task.FromResult<(TimestampResult, byte[])>((TimestampResult.Failed, null));
                }
                using (context)
                {
                    var refSuccess = false;
                    try
                    {
                        context.DangerousAddRef(ref refSuccess);
                        if (!refSuccess)
                        {
                            return Task.FromResult<(TimestampResult, byte[])>((TimestampResult.Failed, null));
                        }
                        var structure = Marshal.PtrToStructure<CRYPT_TIMESTAMP_CONTEXT>(context.DangerousGetHandle());
                        var encoded = new byte[structure.cbEncoded];
                        Marshal.Copy(structure.pbEncoded, encoded, 0, encoded.Length);
                        return Task.FromResult<(TimestampResult, byte[])>((TimestampResult.Success, encoded));
                    }
                    finally
                    {
                        if (refSuccess)
                        {
                            context.DangerousRelease();
                        }
                    }
                }
            }
        }
    }
}
