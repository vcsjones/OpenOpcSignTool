using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    public interface IXmlDSigReferencable
    {
        string Id { get; }
    }

    public class XmlDSigBuilder : IXmlDSigReferencable
    {
        private readonly ISigningContext _signingContext;

        public SignedInfoBuilder SignedInfo { get; }
        public string Id => "idPackageSignature";

        public XmlDSigBuilder(ISigningContext signingContext)
        {
            _signingContext = signingContext;
            SignedInfo = new SignedInfoBuilder(this);
        }

        public async Task<XmlDocument> BuildAsync()
        {
            return null;
        }

        public class SignedInfoBuilder
        {
            private readonly XmlDSigBuilder _builder;

            internal SignedInfoBuilder(XmlDSigBuilder builder)
            {
                _builder = builder;
            }

            public IList<XmlDSigObject> Objects { get; } = new List<XmlDSigObject>();
        }
    }

    public class XmlDSigObject : IXmlDSigReferencable
    {
        private readonly List<XmlDSigSignatureProperty> _properties = new List<XmlDSigSignatureProperty>();

        public string Id { get; }

        public XmlDSigObject(string id)
        {
            Id = id;
        }

        public void AddSignatureProperty(IXmlDSigReferencable target, string id, object contents) =>
            _properties.Add(new XmlDSigSignatureProperty(target, id, contents));
    }

    internal class XmlDSigSignatureProperty
    {
        public object Contents { get; }
        public IXmlDSigReferencable Target { get; }
        public string Id { get; }

        public XmlDSigSignatureProperty(IXmlDSigReferencable target, string id, object contents)
        {
            Target = target;
            Contents = contents;
            Id = id;
        }
    }

    public sealed class XmlDSigObjectManifest : XmlDSigObject
    {
        private readonly Dictionary<OpcPart, bool> _parts = new Dictionary<OpcPart, bool>();

        public XmlDSigObjectManifest() : base("idPackageObject")
        {
        }

        /// <summary>
        /// Adds a part to the object manifest.
        /// </summary>
        /// <param name="part">The part to add.</param>
        public void AddPart(OpcPart part)
        {
            if (!_parts.ContainsKey(part))
            {
                _parts.Add(part, false);
            }
        }

        /// <summary>
        /// Adds a part to the object manifest, and all parts with
        /// a relationship to the part. This includes adding the
        /// relationship part itself for this part.
        /// </summary>
        /// <param name="part">The part to add.</param>
        public void AddPartWithRelationships(OpcPart part)
        {
            //We do not do any of the crawling of relationships here because we don't
            //want to materialize the relationships until we are actually ready to do the
            //signing.
            if (!_parts.ContainsKey(part))
            {
                _parts.Add(part, true);
            }
        }
    }
}
