using System;

namespace OpenVsixSignTool
{
    internal static class OpcKnownUris
    {
        public static readonly Uri DigitalSignatureOrigin = new Uri("http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/origin", UriKind.Absolute);
        public static readonly Uri DigitalSignatureSignature = new Uri("http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/signature", UriKind.Absolute);

        public static readonly Uri XmlDSig = new Uri("http://www.w3.org/2000/09/xmldsig#", UriKind.Absolute);
        public static readonly Uri XmlDigitalSignature = new Uri("http://schemas.openxmlformats.org/package/2006/digital-signature", UriKind.Absolute);
        public static readonly Uri XmlDSigObject = new Uri("http://www.w3.org/2000/09/xmldsig#Object", UriKind.Absolute);
    }
}
