using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace OpenVsixSignTool.Core
{
    namespace Internal
    {
        [XmlRoot("Signature", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public sealed class XmlSignatureElement
        {
            [XmlAttribute]
            public string Id { get; set; }
            public XmlSignedInfoElement SignedInfo { get; set; }

            [XmlElement("Object")]
            public List<XmlObjectManifestElement> Objects { get; set; } = new List<XmlObjectManifestElement>();
        }


        public sealed class XmlSignedInfoElement
        {
            [XmlElement]
            public XmlCanonicalizationMethodElement CanonicalizationMethod { get; set; }
        }

        public sealed class XmlCanonicalizationMethodElement
        {
            [XmlAttribute]
            public string Algorithm { get; set; }
        }


        public sealed class XmlSignatureMethodElement
        {
            [XmlAttribute]
            public string Algorithm { get; set; }
        }

        public sealed class XmlObjectManifestElement
        {
            [XmlAttribute]
            public string Id { get; set; }

            [XmlArray(ElementName = "SignatureProperties")]
            [XmlArrayItem(ElementName = "SignatureProperty")]
            public List<XmlSignaturePropertyElement> SignatureProperties { get; set; }

            [XmlArray(ElementName = "Manifest")]
            [XmlArrayItem(ElementName = "Reference")]
            public List<XmlReferenceElement> Manifest { get; set; } = new List<XmlReferenceElement>();

        }


        public sealed class XmlReferenceElement
        {
            [XmlAttribute]
            public string Type { get; set; }

            [XmlAttribute(AttributeName = "URI")]
            public string Uri { get; set; }

            [XmlElement]
            public XmlDigestMethodElement DigestMethod { get; set; }

            [XmlElement]
            public string DigestValue { get; set; }

        }

        public sealed class XmlSignaturePropertyElement
        {
            public string Id { get; set; }
            public string Target { get; set; }
        }

        public sealed class XmlDigestMethodElement
        {
            [XmlAttribute]
            public string Algorithm { get; set; }
        }
    }

    public interface IXmlDSigReferencable
    {
        string Id { get; }
    }

    public class XmlDSigBuilder : IXmlDSigReferencable
    {
        private readonly ISigningContext _signingContext;

        public SignedInfoBuilder SignedInfo { get; }
        public string Id => "idPackageSignature";
        public IList<XmlDSigObjectBase> Objects { get; } = new List<XmlDSigObjectBase>();


        public XmlDSigBuilder(ISigningContext signingContext)
        {
            _signingContext = signingContext;
            SignedInfo = new SignedInfoBuilder(this);
        }

        public async Task<XmlDocument> BuildAsync()
        {
            using (var memoryStream = new MemoryStream())
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "http://www.w3.org/2000/09/xmldsig#");
                var serializer = new XmlSerializer(typeof(Internal.XmlSignatureElement));
                var element = new Internal.XmlSignatureElement
                {
                    Id = Id,
                    SignedInfo = await SignedInfo.Build()
                };

                foreach(var obj in Objects)
                {
                    element.Objects.Add(await obj.Build());
                }

                serializer.Serialize(memoryStream, element, ns);
                memoryStream.Position = 0;
                var document = new XmlDocument();
                document.Load(memoryStream);
                return document;
            }
        }

        public class SignedInfoBuilder
        {
            private readonly XmlDSigBuilder _builder;

            internal SignedInfoBuilder(XmlDSigBuilder builder)
            {
                _builder = builder;
            }

            internal async Task<Internal.XmlSignedInfoElement> Build()
            {
                var element = new Internal.XmlSignedInfoElement();
                return element;
            }
        }
    }

    public abstract class XmlDSigObjectBase : IXmlDSigReferencable
    {
        private readonly List<XmlDSigSignatureProperty> _properties = new List<XmlDSigSignatureProperty>();

        public string Id { get; }

        protected XmlDSigObjectBase(string id)
        {
            Id = id;
        }

        public void AddSignatureProperty(IXmlDSigReferencable target, string id, object contents) =>
            _properties.Add(new XmlDSigSignatureProperty(target, id, contents));

        internal abstract Task<Internal.XmlObjectManifestElement> Build();
    }

    public sealed class XmlDSigObject : XmlDSigObjectBase
    {

        public XmlDSigObject(string id) : base(id)
        {
        }

        internal override async Task<Internal.XmlObjectManifestElement> Build()
        {
            return new Internal.XmlObjectManifestElement()
            {
                SignatureProperties = new List<Internal.XmlSignaturePropertyElement>
                {
                    new Internal.XmlSignaturePropertyElement(),
                    new Internal.XmlSignaturePropertyElement(),
                },
                Id = Id
            };
        }
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

    public sealed class XmlDSigObjectManifestBuilder : XmlDSigObjectBase
    {
        private readonly Dictionary<OpcPart, bool> _parts = new Dictionary<OpcPart, bool>();

        public XmlDSigObjectManifestBuilder() : base("idPackageObject")
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

        internal override async Task<Internal.XmlObjectManifestElement> Build()
        {
            var manifestObject = new Internal.XmlObjectManifestElement
            {
                Id = Id
            };
            foreach(var (part, includeRelationship) in _parts)
            {
                var (digest, identifer) = OpcPartDigestProcessor.Digest(part, HashAlgorithmName.SHA256);
                var builder = new UriBuilder(part.Uri);
                builder.Query = "ContentType=" + part.ContentType;
                manifestObject.Manifest.Add(new Internal.XmlReferenceElement
                {
                    DigestMethod = new Internal.XmlDigestMethodElement
                    {
                        Algorithm = identifer.AbsoluteUri
                    },
                    DigestValue = Convert.ToBase64String(digest),
                    Uri = builder.Uri.ToQualifiedPath()
                });
            }
            return manifestObject;
        }
    }

    internal static class CanonicalizationHelper
    {
        public static Stream CanonicalizeElement(XmlElement element, Internal.XmlCanonicalizationMethodElement canonicalizationMethod)
        {
            //The canonicalization transformer can't reasonably do just an element. It
            //seems content to do an entire XmlDocument.
            var transformer = new XmlDsigC14NTransform(false);

            var newDocument = new XmlDocument(element.OwnerDocument.NameTable);
            newDocument.LoadXml(element.OuterXml);

            transformer.LoadInput(newDocument);

            var result = transformer.GetOutput(typeof(Stream));
            canonicalizationMethod.Algorithm = transformer.Algorithm;
            if (result is Stream s)
            {
                return s;
            }
            throw new NotSupportedException("Unable to canonicalize element.");
        }
    }
}
