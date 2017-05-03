using System;
using System.Collections.Generic;

namespace OpenVsixSignTool.Core
{
    internal class OpcSignatureManifest
    {
        private readonly List<OpcPartDigest> _digests;

        private OpcSignatureManifest(List<OpcPartDigest> digests)
        {
            _digests = digests;
        }

        public static OpcSignatureManifest Build(CertificateSigningContext context, IEnumerable<OpcPart> parts)
        {
            var digests = new List<OpcPartDigest>();
            foreach (var part in parts)
            {
                var (digest, identifier) = OpcPartDigestProcessor.Digest(part, context.FileDigestAlgorithmName);
                var builder = new UriBuilder(part.Uri);
                builder.Query = "ContentType=" + part.ContentType;
                digests.Add(new OpcPartDigest(builder.Uri, identifier, digest));
            }
            return new OpcSignatureManifest(digests);
        }

        public IReadOnlyList<OpcPartDigest> Manifest => _digests;
    }
}
