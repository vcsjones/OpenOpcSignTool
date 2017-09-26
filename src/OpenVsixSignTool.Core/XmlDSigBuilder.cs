using System;
using System.Collections.Generic;
using System.IO;
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

            [XmlElement("Reference")]
            public List<XmlSignedInfoReferenceElement> References { get; set; }
        }

        public sealed class XmlSignedInfoReferenceElement
        {

            [XmlAttribute("URI")]
            public string Uri { get; set; }

            [XmlAttribute]
            public string Type { get; set; }

            public XmlDigestMethodElement DigestMethod { get; set; }
            public string DigestValue { get; set; }
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

            [XmlArray(ElementName = "Manifest")]
            [XmlArrayItem(ElementName = "Reference")]
            public List<XmlReferenceElement> Manifest { get; set; } = new List<XmlReferenceElement>();

            [XmlArray(ElementName = "SignatureProperties")]
            [XmlArrayItem(ElementName = "SignatureProperty")]
            public List<XmlSignaturePropertyElement> SignatureProperties { get; set; }
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

        [XmlInclude(typeof(XmlSignatureTimeElement))]
        public sealed class XmlSignaturePropertyElement
        {
            [XmlAttribute]
            public string Id { get; set; }

            [XmlAttribute]
            public string Target { get; set; }

            [XmlElement(typeof(XmlSignatureTimeElement),
                ElementName = "SignatureTime",
                Namespace = "http://schemas.openxmlformats.org/package/2006/digital-signature")]
            public object Contents { get; set; }
        }

        public sealed class XmlDigestMethodElement
        {
            [XmlAttribute]
            public string Algorithm { get; set; }
        }

        public sealed class XmlSignatureTimeElement
        {
            public string Format { get; set; }
            public string Value { get; set; }
        }
    }

    public interface IXmlDSigReferencable
    {
        string Id { get; }
        string Type { get; }
    }

    public class XmlDSigBuilder : IXmlDSigReferencable
    {
        private readonly ISigningContext _signingContext;

        public SignedInfoBuilder SignedInfo { get; }
        public string Id => "idPackageSignature";
        public string Type => null;
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

                foreach (var obj in Objects)
                {
                    element.Objects.Add(await obj.Build(_signingContext));
                }

                serializer.Serialize(memoryStream, element, ns);
                memoryStream.Position = 0;
                var document = new XmlDocument();
                document.Load(memoryStream);
                await CompleteSigning(document);
                return document;
            }
        }

        public Task CompleteSigning(XmlDocument document)
        {
            return Task.CompletedTask;
        }

        public class SignedInfoBuilder
        {
            private readonly XmlDSigBuilder _builder;
            private readonly List<IXmlDSigReferencable> _references = new List<IXmlDSigReferencable>();

            public void AddReference(IXmlDSigReferencable reference)
            {
                _references.Add(reference);
            }

            internal SignedInfoBuilder(XmlDSigBuilder builder)
            {
                _builder = builder;
            }

            internal Task<Internal.XmlSignedInfoElement> Build()
            {
                var element = new Internal.XmlSignedInfoElement
                {
                    References = new List<Internal.XmlSignedInfoReferenceElement>()
                };
                foreach (var reference in _references)
                {
                    element.References.Add(new Internal.XmlSignedInfoReferenceElement
                    {
                        Uri = $"#{reference.Id}",
                        Type = reference.Type
                    });
                }
                return Task.FromResult(element);
            }
        }
    }

    public abstract class XmlDSigObjectBase : IXmlDSigReferencable
    {
        private readonly List<XmlDSigSignatureProperty> _properties = new List<XmlDSigSignatureProperty>();

        public string Id { get; }
        public string Type { get; }

        protected XmlDSigObjectBase(string id, string type)
        {
            Id = id;
            Type = type;
        }

        internal IReadOnlyList<XmlDSigSignatureProperty> Properties => _properties;

        public void AddSignatureProperty(IXmlDSigReferencable target, string id, IXmlObjectProperty contents) =>
            _properties.Add(new XmlDSigSignatureProperty(target, id, contents));

        internal abstract Task<Internal.XmlObjectManifestElement> Build(ISigningContext context);
    }

    internal class XmlDSigSignatureProperty
    {
        public IXmlObjectProperty Contents { get; }
        public IXmlDSigReferencable Target { get; }
        public string Id { get; }

        public XmlDSigSignatureProperty(IXmlDSigReferencable target, string id, IXmlObjectProperty contents)
        {
            Target = target;
            Contents = contents;
            Id = id;
        }
    }

    public sealed class XmlDSigObjectManifestBuilder : XmlDSigObjectBase
    {
        private readonly Dictionary<OpcPart, bool> _parts = new Dictionary<OpcPart, bool>();

        public XmlDSigObjectManifestBuilder() : base("idPackageObject", "http://www.w3.org/2000/09/xmldsig#Object")
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

        internal override Task<Internal.XmlObjectManifestElement> Build(ISigningContext context)
        {
            var manifestObject = new Internal.XmlObjectManifestElement
            {
                Id = Id
            };
            foreach (var (part, _) in _parts)
            {
                var (digest, identifer) = OpcPartDigestProcessor.Digest(part, context.FileDigestAlgorithmName);
                var builder = new UriBuilder(part.Uri)
                {
                    Query = "ContentType=" + part.ContentType
                };
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
            if (Properties.Count > 0)
            {
                manifestObject.SignatureProperties = new List<Internal.XmlSignaturePropertyElement>();
                foreach (var property in Properties)
                {
                    manifestObject.SignatureProperties.Add(new Internal.XmlSignaturePropertyElement
                    {
                        Id = property.Id,
                        Target = property.Target?.Id ?? "",
                        Contents = property.Contents.ToNativeFormat()
                    });
                }
            }
            return Task.FromResult(manifestObject);

        }
    }

    public class XmlSignatureTimeSignatureProperty : IXmlObjectProperty
    {
        public DateTimeOffset Value { get; set; }
        public string Format { get; } = "YYYY-MM-DDThh:mm:ss.sTZD";

        object IXmlObjectProperty.ToNativeFormat()
        {
            return new Internal.XmlSignatureTimeElement
            {
                Format = Format,
                Value = Value.ToString("yyyy-MM-ddTHH:mm:ss.fzzz")
            };
        }
    }

    public interface IXmlObjectProperty
    {
        object ToNativeFormat();
    }


    internal static class CanonicalizationHelper
    {
        public static Stream CanonicalizeElement(XmlElement element, Internal.XmlCanonicalizationMethodElement canonicalizationMethod)
        {
            if (element.OwnerDocument == null)
            {
                throw new InvalidOperationException("Cannot canonicalize detached element.");
            }
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
