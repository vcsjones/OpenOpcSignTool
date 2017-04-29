using System;

namespace OpenVsixSignTool.Core
{
    internal class OpcPartDigest
    {
        public Uri ReferenceUri { get; }
        public Uri DigestAlgorithmIdentifier { get; }
        public byte[] Digest { get; }

        public OpcPartDigest(Uri referenceUri, Uri digestAlgorithmIdentifer, byte[] digest)
        {
            ReferenceUri = referenceUri;
            DigestAlgorithmIdentifier = digestAlgorithmIdentifer;
            Digest = digest;
        }
    }

}
