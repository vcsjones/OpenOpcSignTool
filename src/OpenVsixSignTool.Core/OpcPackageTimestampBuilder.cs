using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

#if NETSTANDARD2_0
using System.Net;
using System.Net.Http;
using Org.BouncyCastle.Tsp;
using Org.BouncyCastle.Asn1;
#elif NET462
using OpenVsixSignTool.Core.Interop;
#endif

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// A builder for adding timestamps to a package.
    /// </summary>
    public class OpcPackageTimestampBuilder
    {
        private readonly OpcPart _part;

        internal OpcPackageTimestampBuilder(OpcPart part)
        {
            _part = part;
            Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Gets or sets the timeout for signing the package.
        /// The default is 30 earth seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Signs the package with a timestamp.
        /// </summary>
        /// <param name="timestampServer">The URI of the timestamp server.</param>
        /// <param name="timestampAlgorithm">The hash algorithm to timestamp with.</param>
        /// <returns>A result of the timestamp operation.</returns>
        public Task<TimestampResult> SignAsync(Uri timestampServer, HashAlgorithmName timestampAlgorithm)
        {
            if (timestampServer == null)
            {
                throw new ArgumentNullException(nameof(timestampServer));
            }
            if (!timestampServer.IsAbsoluteUri)
            {
                throw new ArgumentException("The timestamp server must be an absolute URI.", nameof(timestampServer));
            }
            using (var nonce = new TimestampNonceFactory())
            {
#if NET462
                return Win32TimeStamp(timestampServer, timestampAlgorithm, nonce);
#elif NETSTANDARD2_0
                return BouncyCastleTimeStamp(timestampServer, timestampAlgorithm, nonce);
#else
                throw new PlatformNotSupportedException("Timestamping is not supported on this platform.");
#endif
            }
        }

#if NETSTANDARD2_0
        private async Task<TimestampResult> BouncyCastleTimeStamp(Uri timestampServer, HashAlgorithmName timestampAlgorithm, TimestampNonceFactory nonce)
        {
            var oid = HashAlgorithmTranslator.TranslateFromNameToOid(timestampAlgorithm);
            var requestGenerator = new TimeStampRequestGenerator();
            var (signatureDocument, timestampSubject) = GetSignatureToTimestamp(_part);
            using (var hash = HashAlgorithmTranslator.TranslateFromNameToxmlDSigUri(timestampAlgorithm, out _))
            {
                var digest = hash.ComputeHash(timestampSubject);
                var request = requestGenerator.Generate(oid.Value, digest, new Org.BouncyCastle.Math.BigInteger(nonce.Nonce));
                var encodedRequest = request.GetEncoded();
                var client = new HttpClient();
                var content = new ByteArrayContent(encodedRequest);
                content.Headers.Add("Content-Type", "application/timestamp-query");
                var post = await client.PostAsync(timestampServer, content);
                if (post.StatusCode != HttpStatusCode.OK)
                {
                    return TimestampResult.Failed;
                }
                var responseBytes = await post.Content.ReadAsByteArrayAsync();
                var responseParser = new Asn1StreamParser(responseBytes);
                var timeStampResponse = new TimeStampResponse(responseBytes);
                var tokenResponse = timeStampResponse.TimeStampToken.GetEncoded();
                ApplyTimestamp(signatureDocument, _part, tokenResponse);
                return TimestampResult.Success;
            }
        }
#endif

#if NET462
        private Task<TimestampResult> Win32TimeStamp(Uri timestampServer, HashAlgorithmName timestampAlgorithm, TimestampNonceFactory nonce)
        {
            var oid = HashAlgorithmTranslator.TranslateFromNameToOid(timestampAlgorithm);
            var parameters = new CRYPT_TIMESTAMP_PARA
            {
                cExtension = 0,
                fRequestCerts = true
            };
            parameters.Nonce.cbData = nonce.Size;
            parameters.Nonce.pbData = nonce.NoncePointer;
            parameters.pszTSAPolicyId = null;
            var (signatureDocument, timestampSubject) = GetSignatureToTimestamp(_part);
            var winResult = Crypt32.CryptRetrieveTimeStamp(
                timestampServer.AbsoluteUri,
                CryptRetrieveTimeStampRetrievalFlags.NONE,
                (uint)Timeout.TotalMilliseconds,
                oid.Value,
                ref parameters,
                timestampSubject,
                (uint)timestampSubject.Length,
                out var context,
                IntPtr.Zero,
                IntPtr.Zero
            );
            if (!winResult)
            {
                return Task.FromResult(TimestampResult.Failed);
            }
            using (context)
            {
                var refSuccess = false;
                try
                {
                    context.DangerousAddRef(ref refSuccess);
                    if (!refSuccess)
                    {
                        return Task.FromResult(TimestampResult.Failed);
                    }
                    var structure = Marshal.PtrToStructure<CRYPT_TIMESTAMP_CONTEXT>(context.DangerousGetHandle());
                    var encoded = new byte[structure.cbEncoded];
                    Marshal.Copy(structure.pbEncoded, encoded, 0, encoded.Length);
                    ApplyTimestamp(signatureDocument, _part, encoded);
                    return Task.FromResult(TimestampResult.Success);
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
#endif

        private static (XDocument document, byte[] signature) GetSignatureToTimestamp(OpcPart signaturePart)
        {
            XNamespace xmlDSigNamespace = OpcKnownUris.XmlDSig.AbsoluteUri;
            using (var signatureStream = signaturePart.Open())
            {
                var doc = XDocument.Load(signatureStream);
                var signature = doc.Element(xmlDSigNamespace + "Signature")?.Element(xmlDSigNamespace + "SignatureValue")?.Value?.Trim();
                return (doc, Convert.FromBase64String(signature));
            }
        }

        private static void ApplyTimestamp(XDocument originalSignatureDocument, OpcPart signaturePart, byte[] timestampSignature)
        {
            XNamespace xmlDSigNamespace = OpcKnownUris.XmlDSig.AbsoluteUri;
            XNamespace xmlSignatureNamespace = OpcKnownUris.XmlDigitalSignature.AbsoluteUri;
            var document = new XDocument(originalSignatureDocument);
            var signature = new XElement(xmlDSigNamespace + "Object",
                new XElement(xmlSignatureNamespace + "TimeStamp", new XAttribute("Id", "idSignatureTimestamp"),
                    new XElement(xmlSignatureNamespace + "Comment", ""),
                    new XElement(xmlSignatureNamespace + "EncodedTime", Convert.ToBase64String(timestampSignature))
                )
            );
            document.Element(xmlDSigNamespace + "Signature").Add(signature);
            using (var copySignatureStream = signaturePart.Open())
            {
                using (var xmlWriter = new XmlTextWriter(copySignatureStream, System.Text.Encoding.UTF8))
                {
                    //The .NET implementation of OPC used by Visual Studio does not tollerate "white space" nodes.
                    xmlWriter.Formatting = Formatting.None;
                    document.Save(xmlWriter);
                }
            }
        }

        internal class TimestampNonceFactory : IDisposable
        {
            private readonly IntPtr _nativeMemory;
            private readonly uint _nonceSize;
            private readonly byte[] _nonce;

            public TimestampNonceFactory(int nonceSize = 32)
            {
                _nativeMemory = Marshal.AllocCoTaskMem(nonceSize);
                _nonceSize = checked((uint)nonceSize);
                _nonce = new byte[nonceSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(_nonce);
                }
                //The nonce is technically an integer. Some timestamp servers may not like a "negative" nonce. Clear the sign bit so it's positive.
                //That loses one bit of entropy, however is well within the security boundary of a properly sized nonce. Authenticode doesn't even use
                //a nonce.
                _nonce[_nonce.Length - 1] &= 0b01111111;
                Marshal.Copy(_nonce, 0, _nativeMemory, _nonce.Length);
            }

            public IntPtr NoncePointer => _nativeMemory;
            public byte[] Nonce => _nonce;
            public uint Size => _nonceSize;

            public void Dispose()
            {
                Marshal.FreeCoTaskMem(_nativeMemory);
            }
        }
    }
}
