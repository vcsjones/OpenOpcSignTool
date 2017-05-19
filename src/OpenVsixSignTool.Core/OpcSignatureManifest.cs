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

        public static OpcSignatureManifest Build(ISigningContext context, HashSet<OpcPart> parts)
        {
            var digests = new List<OpcPartDigest>(parts.Count);
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
