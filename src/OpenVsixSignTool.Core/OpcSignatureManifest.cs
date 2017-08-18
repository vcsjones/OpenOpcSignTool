using System;
using System.Collections.Generic;

namespace OpenVsixSignTool.Core
{
    internal class OpcSignatureManifest
    {
        private readonly List<OpcPartDigest> _digests;
        private readonly HashSet<OpcPart> _parts;

        private OpcSignatureManifest(List<OpcPartDigest> digests, HashSet<OpcPart> parts)
        {
            _digests = digests;
            _parts = parts;
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
            return new OpcSignatureManifest(digests, parts);
        }

        public IReadOnlyList<OpcPartDigest> Manifest => _digests;
        public HashSet<OpcPart> Parts => _parts;
    }
}
