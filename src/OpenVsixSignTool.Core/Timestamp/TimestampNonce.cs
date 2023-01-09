using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool.Core.Timestamp
{
    internal readonly struct TimestampNonce
    {
        public ReadOnlyMemory<byte> Nonce { get; }

        public TimestampNonce(ReadOnlyMemory<byte> nonce)
        {
            Nonce = nonce;
        }

        public static TimestampNonce Generate(int nonceSize = 32)
        {
            var nonce = new byte[nonceSize];
#if NET
                RandomNumberGenerator.Fill(nonce);
#else
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }
#endif
            //The nonce is technically an integer. Some timestamp servers may not like a "negative" nonce. Clear the sign bit so it's positive.
            //That loses one bit of entropy, however is well within the security boundary of a properly sized nonce. Authenticode doesn't even use
            //a nonce.
            nonce[nonce.Length - 1] &= 0b01111111;
            return new TimestampNonce(nonce);
        }
    }
}
