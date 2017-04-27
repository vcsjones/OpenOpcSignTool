using OpenVsixSignTool.Interop;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OpenVsixSignTool
{
    /// <summary>
    /// A builder for adding timestamps to a package.
    /// </summary>
    public class OpcPackageTimestampBuilder
    {
        private readonly OpcPackage _package;

        internal OpcPackageTimestampBuilder(OpcPackage package)
        {
            _package = package;
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
        public TimestampResult Sign(Uri timestampServer, HashAlgorithmName timestampAlgorithm)
        {
            if (timestampServer == null)
            {
                throw new ArgumentNullException(nameof(timestampServer));
            }
            if (!timestampServer.IsAbsoluteUri)
            {
                throw new ArgumentException("The timestamp server must be an absolute URI.", nameof(timestampServer));
            }
            var signaturePart = _package.GetSignaturePart();
            if (signaturePart == null)
            {
                return TimestampResult.PackageNotSigned;
            }
            var oid = HashAlgorithmTranslator.TranslateFromNameToOid(timestampAlgorithm);
            using (var nonce = new TimestampNonceFactory())
            {
                var parameters = new CRYPT_TIMESTAMP_PARA();
                parameters.cExtension = 0;
                parameters.fRequestCerts = true;
                parameters.Nonce.cbData = nonce.Size;
                parameters.Nonce.pbData = nonce.Nonce;
                parameters.pszTSAPolicyId = null;
                var (signatureDocument, timestampSubject) = GetSignatureToTimestamp(signaturePart);
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
                    return TimestampResult.Failed;
                }
                using (context)
                {
                    var refSuccess = false;
                    try
                    {
                        context.DangerousAddRef(ref refSuccess);
                        if (!refSuccess)
                        {
                            return TimestampResult.Failed;
                        }
                        var structure = Marshal.PtrToStructure<CRYPT_TIMESTAMP_CONTEXT>(context.DangerousGetHandle());
                        var encoded = new byte[structure.cbEncoded];
                        Marshal.Copy(structure.pbEncoded, encoded, 0, encoded.Length);
                        ApplyTimestamp(signatureDocument, signaturePart, encoded);
                        return TimestampResult.Success;
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

            public TimestampNonceFactory(int nonceSize = 32)
            {
                _nativeMemory = Marshal.AllocCoTaskMem(nonceSize);
                _nonceSize = checked((uint)nonceSize);
                var nonce = new byte[nonceSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }
                //The nonce is technically an integer. Some timestamp servers may not like a "negative" nonce. Clear the sign bit so it's positive.
                //That loses one bit of entropy, however is well within the security boundary of a properly sized nonce. Authenticode doesn't even use
                //a nonce.
                nonce[nonce.Length - 1] &= 0b01111111;
                Marshal.Copy(nonce, 0, _nativeMemory, nonce.Length);
            }

            public IntPtr Nonce => _nativeMemory;
            public uint Size => _nonceSize;

            public void Dispose()
            {
                Marshal.FreeCoTaskMem(_nativeMemory);
            }
        }
    }
}
