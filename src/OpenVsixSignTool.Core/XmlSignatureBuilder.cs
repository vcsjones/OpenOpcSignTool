using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    internal class XmlSignatureBuilder
    {
        private readonly XmlDocument _document;
        private readonly ISigningContext _signingContext;
        private readonly XmlElement _signatureElement;
        private XmlElement _objectElement;


        /// <summary>
        /// Creates a new signature with the correct namespace and empty root <c>Signature</c> element.
        /// </summary>
        internal XmlSignatureBuilder(ISigningContext signingContext)
        {
            _signingContext = signingContext;
            _document = new XmlDocument();
            var manager = new XmlNamespaceManager(_document.NameTable);
            manager.AddNamespace("", OpcKnownUris.XmlDSig.AbsoluteUri);
            _signatureElement = CreateDSigElement("Signature");
            _signatureElement.SetAttribute("Id", "SignatureIdValue");
        }

        static int GetNonXmlnsAttributeCount(XmlReader reader)
        {
            // Debug.Assert(reader != null, "xmlReader should not be null");
            // Debug.Assert(reader.NodeType == XmlNodeType.Element, "XmlReader should be positioned at an Element");

            int readerCount = 0;

            //If true, reader moves to the attribute
            //If false, there are no more attributes (or none)
            //and in that case the position of the reader is unchanged.
            //First time through, since the reader will be positioned at an Element,
            //MoveToNextAttribute is the same as MoveToFirstAttribute.
            while (reader.MoveToNextAttribute())
            {
                if (String.CompareOrdinal(reader.Name, "xmlns") != 0 &&
                    String.CompareOrdinal(reader.Prefix, "xmlns") != 0)
                    readerCount++;
            }

            //re-position the reader to the element
            reader.MoveToElement();

            return readerCount;
        }

        /// <summary>
        /// Parse the Relationship-specific Transform
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="partUri"></param>
        /// <param name="relationshipSelectors">may be allocated but will never be empty</param>
        private static void ParseRelationshipsTransform(XmlReader reader, Uri partUri, ref List<OpcRelationship> relationshipSelectors)
        {
            Uri owningPartUri = partUri;

            // find all of the Relationship tags of form:
            //      <RelationshipReference SourceId="abc123" />
            // or 
            //      <RelationshipsGroupReference SourceType="reference-type-of-the-week" />
            while (reader.Read() && (reader.MoveToContent() == XmlNodeType.Element)
                && reader.Depth == 5)
            {
                // both types have no children, a single required attribute and belong to the OPC namespace
                if (reader.IsEmptyElement
                    && GetNonXmlnsAttributeCount(reader) == 1
                    && (String.CompareOrdinal(reader.NamespaceURI, XTable.Get(XTable.ID.OpcSignatureNamespace)) == 0))
                {
                    // <RelationshipReference>?
                    if (String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.RelationshipReferenceTagName)) == 0)
                    {
                        // RelationshipReference tags are legal and these must be empty with a single SourceId attribute
                        // get the SourceId attribute 
                        string id = reader.GetAttribute(XTable.Get(XTable.ID.SourceIdAttrName));
                        if (id != null && id.Length > 0)
                        {
                            if (relationshipSelectors == null)
                                relationshipSelectors = new List<OpcRelationship>();

                            // we found a legal SourceId so create a selector and continue searching
                            relationshipSelectors.Add(new OpcRelationship(owningPartUri, id, new Uri("Id")));
                            continue;
                        }
                    }   // <RelationshipsGroupReference>?
                    else if ((String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.RelationshipsGroupReferenceTagName)) == 0))
                    {
                        // RelationshipsGroupReference tags must be empty with a single SourceType attribute
                        string type = reader.GetAttribute(XTable.Get(XTable.ID.SourceTypeAttrName));
                        if (type != null && type.Length > 0)
                        {
                            // lazy init
                            if (relationshipSelectors == null)
                                relationshipSelectors = new List<OpcRelationship>();

                            // got a legal SourceType attribute
                            relationshipSelectors.Add(new OpcRelationship(owningPartUri, type, new Uri("Type")));
                            continue;
                        }
                    }
                }

                // if we get to here, we have not found a legal tag so we throw
                throw new XmlException("UnexpectedXmlTag" + reader.LocalName);
            }
        }

        // As per the OPC spec, only two tranforms are valid. Also, both of these happen to be
        // XML canonicalization transforms.
        // In the XmlSignatureManifest.ParseTransformsTag method we make use this method to 
        // validate the transforms to make sure that they are supported by the OPC spec and
        // we also take advantage of the fact that both of them are XML canonicalization transforms
        // IMPORTANT NOTE:
        // 1. In the XmlDigitalSignatureProcessor.StringToTransform method, we have similar logic
        // regarding these two transforms.So both these methods must be updated in sync.
        // 2. If ever this method is updated to add other transforms, careful review must be done to 
        // make sure that methods calling this method are updated as required.
        internal static bool IsValidXmlCanonicalizationTransform(String transformName)
        {
            if (String.CompareOrdinal(transformName, SignedXml.XmlDsigC14NTransformUrl) == 0 ||
                String.CompareOrdinal(transformName, SignedXml.XmlDsigC14NWithCommentsTransformUrl) == 0)
            {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Parses Transforms tag
        /// </summary>
        /// <param name="reader">node to parse</param>
        /// <param name="partUri">Part Uri for the part owning the relationships</param>
        /// <param name="relationshipSelectors">allocates and returns a list of 
        /// PackageRelationshipSelectors if Relationship transform</param>
        /// <returns>ordered list of Transform names</returns>
        private static List<String> ParseTransformsTag(XmlReader reader, Uri partUri, ref List<OpcRelationship> relationshipSelectors)
        {
            // # reference that signs multiple PackageRelationships
            // <Reference URI="/shared/_rels/image.jpg.rels?ContentType=image/jpg">
            //      <Transforms>
            //          <Transform Algorithm="http://schemas.openxmlformats.org/package/2006/RelationshipTransform">
            //              <RelationshipReference SourceId="1" />
            //              <RelationshipReference SourceId="2" />
            //              <RelationshipReference SourceId="8" />
            //          </Transform>
            //          <Transform Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
            //      </Transforms>
            //      <DigestMethod Algorithm="sha1" />
            //      <DigestValue>... </DigestValue>
            // </Reference>

            List<String> transforms = null;
            bool relationshipTransformFound = false;
            int transformsCountWhenRelationshipTransformFound = 0;

            // Look for transforms.
            // There are currently only 3 legal transforms which can be arranged in any
            // combination.
            while (reader.Read() && (reader.MoveToContent() == XmlNodeType.Element))
            {
                String transformName = null;

                // at this level, all tags must be Transform tags
                if (reader.Depth != 4
                    || String.CompareOrdinal(reader.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) != 0
                    || String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.TransformTagName)) != 0)
                {
                    throw new XmlException("XmlSignatureParseError");
                }

                // inspect the Algorithm attribute to determine the type of transform
                if (GetNonXmlnsAttributeCount(reader) == 1)
                {
                    transformName = reader.GetAttribute(XTable.Get(XTable.ID.AlgorithmAttrName));
                }

                // legal transform name?
                if ((transformName != null) && (transformName.Length > 0))
                {
                    // what type of transform?
                    if (String.CompareOrdinal(transformName, XTable.Get(XTable.ID.RelationshipsTransformName)) == 0)
                    {
                        if (!relationshipTransformFound)
                        {
                            // relationship transform
                            ParseRelationshipsTransform(reader, partUri, ref relationshipSelectors);

                            if (transforms == null)
                                transforms = new List<String>();

                            transforms.Add(transformName);

                            relationshipTransformFound = true;
                            transformsCountWhenRelationshipTransformFound = transforms.Count;
                            continue;   // success
                        }
                        else
                            throw new XmlException("MultipleRelationshipTransformsFound");
                    }
                    else
                    {
                        // non-Relationship transform should have no children
                        if (reader.IsEmptyElement)
                        {
                            if (transforms == null)
                                transforms = new List<String>();

                            if (IsValidXmlCanonicalizationTransform(transformName))
                            {
                                transforms.Add(transformName);  // return it
                                continue;   // success
                            }
                            else
                                throw new InvalidOperationException("UnsupportedTransformAlgorithm");
                        }
                    }
                }
                throw new XmlException("XmlSignatureParseError");
            }

            if (transforms.Count == 0)
                throw new XmlException("XmlSignatureParseError");

            //If we found another transform after the Relationship transform, it will be validated earlier
            //in this method to make sure that its a supported xml canonicalization algorithm and so we can 
            //simplify this test condition - As per the OPC spec - Relationship transform must be followed
            //by a canonicalization algorithm.
            if (relationshipTransformFound && (transforms.Count == transformsCountWhenRelationshipTransformFound))
                throw new XmlException("RelationshipTransformNotFollowedByCanonicalizationTransform");

            return transforms;
        }

        private XmlElement CreateDSigElement(string name) => _document.CreateElement(name, OpcKnownUris.XmlDSig.AbsoluteUri);

        private XmlElement CreateOpcSigElement(string name) => _document.CreateElement(name, XTable.Get(XTable.ID.OpcSignatureNamespace));

        public XmlDocument Build()
        {
            if (_objectElement == null)
            {
                throw new InvalidOperationException("A manifest has not been set on the builder.");
            }
            XmlElement keyInfoElement, signedInfo, signatureValue;
            var info = new HashAlgorithmInfo(_signingContext.FileDigestAlgorithmName);
            using (var canonicalHashAlgorithm = info.Create())
            {
                byte[] objectElementHash;
                string canonicalizationMethodObjectId;
                using (var objectElementCanonicalData = CanonicalizeElement(_objectElement, out canonicalizationMethodObjectId))
                {
                    objectElementHash = canonicalHashAlgorithm.ComputeHash(objectElementCanonicalData);
                }
                keyInfoElement = BuildKeyInfoElement();
                Stream signerInfoCanonicalStream;
                (signerInfoCanonicalStream, signedInfo) = BuildSignedInfoElement(
                    (_objectElement, objectElementHash, info.XmlDSigIdentifier.AbsoluteUri, canonicalizationMethodObjectId)
                );
                byte[] signerInfoElementHash;

                using (signerInfoCanonicalStream)
                {
                    signerInfoElementHash = canonicalHashAlgorithm.ComputeHash(signerInfoCanonicalStream);
                }
                signatureValue = BuildSignatureValue(signerInfoElementHash);
            }

            _signatureElement.AppendChild(signedInfo);
            _signatureElement.AppendChild(signatureValue);
            //_signatureElement.AppendChild(keyInfoElement);
            _signatureElement.AppendChild(_objectElement);
            _document.AppendChild(_signatureElement);

            return _document;
        }

        private XmlElement BuildSignatureValue(byte[] signerInfoElementHash)
        {
            var signatureValueElement = CreateDSigElement("SignatureValue");
            signatureValueElement.InnerText = Convert.ToBase64String(_signingContext.SignDigest(signerInfoElementHash));
            return signatureValueElement;
        }

        private Stream CanonicalizeElement(XmlElement element, out string canonicalizationMethodUri, Action<string> setCanonicalization = null)
        {
            //The canonicalization transformer can't reasonable do just an element. It
            //seems content to do an entire XmlDocument.
            var transformer = new XmlDsigC14NTransform(false);
            setCanonicalization?.Invoke(transformer.Algorithm);

            var newDocument = new XmlDocument(_document.NameTable);
            newDocument.LoadXml(element.OuterXml);

            transformer.LoadInput(newDocument);

            var result = transformer.GetOutput(typeof(Stream));
            canonicalizationMethodUri = transformer.Algorithm;
            if (result is Stream s)
            {
                return s;
            }
            throw new NotSupportedException("Unable to canonicalize element.");
        }

        private Stream cano(XmlElement element, out string canonicalizationMethodUri, Action<string> setCanonicalization = null)
        {
            //The canonicalization transformer can't reasonable do just an element. It
            //seems content to do an entire XmlDocument.
            // var transformer = new XmlDsigC14NTransform(false);
            // setCanonicalization?.Invoke(transformer.Algorithm);

            var newDocument = new XmlDocument(_document.NameTable);
            newDocument.LoadXml(element.OuterXml);

            //  transformer.LoadInput(newDocument);

            var result = newDocument;
            canonicalizationMethodUri = XTable.Get(XTable.ID.RelationshipsTransformName);
            //if (result is Stream s)
            {
                //   return s;
            }
            throw new NotSupportedException("Unable to canonicalize element.");
        }

        private (Stream, XmlElement) BuildSignedInfoElement(params (XmlElement element, byte[] canonicalDigest, string digestAlgorithm, string canonicalizationMethod)[] objects)
        {
            var signingIdentifier = _signingContext.XmlDSigIdentifier;

            var signedInfoElement = CreateDSigElement("SignedInfo");
            var canonicalizationMethodElement = CreateDSigElement("CanonicalizationMethod");
            var canonicalizationMethodAlgorithmAttribute = _document.CreateAttribute("Algorithm");
            canonicalizationMethodElement.Attributes.Append(canonicalizationMethodAlgorithmAttribute);

            var signatureMethodElement = CreateDSigElement("SignatureMethod");
            var signatureMethodAlgorithmAttribute = _document.CreateAttribute("Algorithm");
            signatureMethodAlgorithmAttribute.Value = signingIdentifier.AbsoluteUri;
            signatureMethodElement.Attributes.Append(signatureMethodAlgorithmAttribute);

            signedInfoElement.AppendChild(canonicalizationMethodElement);
            signedInfoElement.AppendChild(signatureMethodElement);

            foreach (var (element, digest, digestAlgorithm, method) in objects)
            {
                var idFromElement = element.GetAttribute("Id");
                var reference = "#" + idFromElement;

                var referenceElement = CreateDSigElement("Reference");
                var referenceUriAttribute = _document.CreateAttribute("URI");
                var referenceTypeAttribute = _document.CreateAttribute("Type");
                referenceUriAttribute.Value = reference;
                referenceTypeAttribute.Value = OpcKnownUris.XmlDSigObject.AbsoluteUri;

                referenceElement.Attributes.Append(referenceUriAttribute);
                referenceElement.Attributes.Append(referenceTypeAttribute);

                // var referencesTransformsElement = CreateDSigElement("Transforms");
                // var transformElement = CreateDSigElement("Transform");
                // var transformAlgorithmAttribute = _document.CreateAttribute("Algorithm");
                // transformAlgorithmAttribute.Value = method;
                // transformElement.Attributes.Append(transformAlgorithmAttribute);
                // referencesTransformsElement.AppendChild(transformElement);
                // referenceElement.AppendChild(referencesTransformsElement);

                var digestMethodElement = CreateDSigElement("DigestMethod");
                var digestMethodAlgorithmAttribute = _document.CreateAttribute("Algorithm");
                digestMethodAlgorithmAttribute.Value = digestAlgorithm;
                digestMethodElement.Attributes.Append(digestMethodAlgorithmAttribute);
                referenceElement.AppendChild(digestMethodElement);

                var digestValueElement = CreateDSigElement("DigestValue");
                digestValueElement.InnerText = Convert.ToBase64String(digest);
                referenceElement.AppendChild(digestValueElement);

                signedInfoElement.AppendChild(referenceElement);
            }

            var canonicalSignerInfo = CanonicalizeElement(signedInfoElement, out _, c => canonicalizationMethodAlgorithmAttribute.Value = c);
            return (canonicalSignerInfo, signedInfoElement);
        }

        private XmlElement BuildKeyInfoElement()
        {
            var publicCertificate = Convert.ToBase64String(_signingContext.Certificate.Export(X509ContentType.Cert));
            var keyInfoElement = CreateDSigElement("KeyInfo");
            var x509DataElement = CreateDSigElement("X509Data");
            var x509CertificateElement = CreateDSigElement("X509Certificate");
            x509CertificateElement.InnerText = publicCertificate;
            x509DataElement.AppendChild(x509CertificateElement);
            keyInfoElement.AppendChild(x509DataElement);
            return keyInfoElement;
        }

        public void SetFileManifest(OpcSignatureManifest manifest, XmlNodeList nodes)
        {
            var objectElement = CreateDSigElement("Object");
            var objectElementId = _document.CreateAttribute("Id");
            objectElementId.Value = "idPackageObject";
            objectElement.Attributes.Append(objectElementId);

            var manifestElement = CreateDSigElement("Manifest");
            manifestElement.SetAttribute(XTable.Get(XTable.ID.OpcSignatureNamespaceAttribute), OpcKnownUris.XmlDigitalSignature.ToString());
            bool partDigestDone = false;
            foreach (var file in manifest.Manifest)
            {
                var referenceElement = CreateDSigElement("Reference");
                var referenceElementUriAttribute = _document.CreateAttribute("URI");
                referenceElementUriAttribute.Value = file.ReferenceUri.ToQualifiedPath();
                referenceElement.Attributes.Append(referenceElementUriAttribute);

                if (file.ReferenceUri.AbsolutePath.Equals("/_rels/.rels") && !partDigestDone)
                {
                    var referencesTransformsElement = CreateDSigElement("Transforms");
                    var transformElement = CreateDSigElement("Transform");
                    var transformAlgorithmAttribute = _document.CreateAttribute("Algorithm");
                    transformAlgorithmAttribute.Value = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
                    transformElement.Attributes.Append(transformAlgorithmAttribute);
                    referencesTransformsElement.AppendChild(transformElement);
                    referenceElement.AppendChild(referencesTransformsElement);

                    partDigestDone = true;
                }
                else if (file.ReferenceUri.AbsolutePath.Equals("/_rels/.rels"))
                {

                    var referencesTransformsElement = CreateDSigElement("Transforms");

                    var transformElement = CreateDSigElement("Transform");
                    var transformAlgorithmAttribute = _document.CreateAttribute("Algorithm");
                    transformAlgorithmAttribute.Value = XTable.Get(XTable.ID.RelationshipsTransformName);
                    transformElement.Attributes.Append(transformAlgorithmAttribute);

                    foreach (XmlNode node in nodes)
                    {
                        var relationshipsGroupReferenceElement = CreateOpcSigElement("opc:RelationshipsGroupReference");
                        var sourceTypeAttribute = _document.CreateAttribute("SourceType");
                        sourceTypeAttribute.Value = node.Attributes["Type"].Value;
                        relationshipsGroupReferenceElement.Attributes.Append(sourceTypeAttribute);
                        transformElement.AppendChild(relationshipsGroupReferenceElement);
                    }

                    referencesTransformsElement.AppendChild(transformElement);

                    //referencesTransformsElement = CreateDSigElement("Transforms");
                    transformElement = CreateDSigElement("Transform");
                    transformAlgorithmAttribute = _document.CreateAttribute("Algorithm");
                    transformAlgorithmAttribute.Value = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
                    transformElement.Attributes.Append(transformAlgorithmAttribute);
                    referencesTransformsElement.AppendChild(transformElement);
                    referenceElement.AppendChild(referencesTransformsElement);
                }

                var digestMethod = CreateDSigElement("DigestMethod");
                var digestMethodAlgorithmAttribute = _document.CreateAttribute("Algorithm");
                digestMethodAlgorithmAttribute.Value = file.DigestAlgorithmIdentifier.AbsoluteUri;
                digestMethod.Attributes.Append(digestMethodAlgorithmAttribute);
                referenceElement.AppendChild(digestMethod);

                var digestValue = CreateDSigElement("DigestValue");
                digestValue.InnerText = System.Convert.ToBase64String(file.Digest);
                referenceElement.AppendChild(digestValue);

                manifestElement.AppendChild(referenceElement);
                objectElement.AppendChild(manifestElement);
            }

            var signaturePropertiesElement = CreateDSigElement("SignatureProperties");
            var signaturePropertyElement = CreateDSigElement("SignatureProperty");
            var signaturePropertyIdAttribute = _document.CreateAttribute("Id");
            var signaturePropertyTargetAttribute = _document.CreateAttribute("Target");
            signaturePropertyIdAttribute.Value = "idSignatureTime";
            signaturePropertyTargetAttribute.Value = "#SignatureIdValue";

            signaturePropertyElement.Attributes.Append(signaturePropertyIdAttribute);
            signaturePropertyElement.Attributes.Append(signaturePropertyTargetAttribute);

            var signatureTimeElement = _document.CreateElement("SignatureTime", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            var signatureTimeFormatElement = _document.CreateElement("Format", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            var signatureTimeValueElement = _document.CreateElement("Value", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            signatureTimeFormatElement.InnerText = "YYYY-MM-DDThh:mm:ss.sTZD";
            signatureTimeValueElement.InnerText = _signingContext.ContextCreationTime.ToString("yyyy-MM-ddTHH:mm:ss.fzzz");

            signatureTimeElement.AppendChild(signatureTimeFormatElement);
            signatureTimeElement.AppendChild(signatureTimeValueElement);

            signaturePropertyElement.AppendChild(signatureTimeElement);
            signaturePropertiesElement.AppendChild(signaturePropertyElement);
            objectElement.AppendChild(signaturePropertiesElement);

            _objectElement = objectElement;
        }
    }
}
