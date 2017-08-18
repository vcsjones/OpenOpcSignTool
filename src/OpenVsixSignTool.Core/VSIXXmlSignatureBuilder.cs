using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    internal abstract class XmlSignatureBuilderBase
    {
        protected readonly XmlDocument Document;
        protected readonly ISigningContext SigningContext;
        protected readonly XmlElement SignatureElement;



        protected XmlSignatureBuilderBase(ISigningContext signingContext)
        {
            SigningContext = signingContext;
            Document = new XmlDocument();
            var manager = new XmlNamespaceManager(Document.NameTable);
            manager.AddNamespace("", OpcKnownUris.XmlDSig.AbsoluteUri);
            SignatureElement = CreateDSigElement("Signature");
        }

        protected XmlElement CreateDSigElement(string name) => Document.CreateElement(name, OpcKnownUris.XmlDSig.AbsoluteUri);


        protected async Task<XmlElement> BuildSignatureValueAsync(byte[] signerInfoElementHash)
        {
            var signatureValueElement = CreateDSigElement("SignatureValue");
            signatureValueElement.InnerText = Convert.ToBase64String(await SigningContext.SignDigestAsync(signerInfoElementHash));
            return signatureValueElement;
        }

        protected Stream CanonicalizeElement(XmlElement element, out string canonicalizationMethodUri, Action<string> setCanonicalization = null)
        {
            //The canonicalization transformer can't reasonably do just an element. It
            //seems content to do an entire XmlDocument.
            var transformer = new XmlDsigC14NTransform(false);
            setCanonicalization?.Invoke(transformer.Algorithm);

            var newDocument = new XmlDocument(Document.NameTable);
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
    }


    internal class VSIXXmlSignatureBuilder : XmlSignatureBuilderBase
    {
        private XmlElement _objectElement;


        /// <summary>
        /// Creates a new signature with the correct namespace and empty root <c>Signature</c> element.
        /// </summary>
        internal VSIXXmlSignatureBuilder(ISigningContext signingContext) : base(signingContext)
        {
        }


        public async Task<XmlDocument> BuildAsync()
        {
            if (_objectElement == null)
            {
                throw new InvalidOperationException("A manifest has not been set on the builder.");
            }
            XmlElement keyInfoElement, signedInfo, signatureValue;
            using (var canonicalHashAlgorithm = HashAlgorithmTranslator.TranslateFromNameToxmlDSigUri(SigningContext.FileDigestAlgorithmName, out var canonicalHashAlgorithmIdentifier))
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
                    (_objectElement, objectElementHash, canonicalHashAlgorithmIdentifier.AbsoluteUri, canonicalizationMethodObjectId)
                );
                byte[] signerInfoElementHash;
                using (signerInfoCanonicalStream)
                {
                    signerInfoElementHash = canonicalHashAlgorithm.ComputeHash(signerInfoCanonicalStream);
                }
                signatureValue = await BuildSignatureValueAsync(signerInfoElementHash);
            }

            SignatureElement.AppendChild(signedInfo);
            SignatureElement.AppendChild(signatureValue);
            SignatureElement.AppendChild(keyInfoElement);
            SignatureElement.AppendChild(_objectElement);
            Document.AppendChild(SignatureElement);
            return Document;
        }

        private (Stream, XmlElement) BuildSignedInfoElement(params (XmlElement element, byte[] canonicalDigest, string digestAlgorithm, string canonicalizationMethod)[] objects)
        {
            var signingIdentifier = SigningContext.XmlDSigIdentifier;

            var signedInfoElement = CreateDSigElement("SignedInfo");
            var canonicalizationMethodElement = CreateDSigElement("CanonicalizationMethod");
            var canonicalizationMethodAlgorithmAttribute = Document.CreateAttribute("Algorithm");
            canonicalizationMethodElement.Attributes.Append(canonicalizationMethodAlgorithmAttribute);

            var signatureMethodElement = CreateDSigElement("SignatureMethod");
            var signatureMethodAlgorithmAttribute = Document.CreateAttribute("Algorithm");
            signatureMethodAlgorithmAttribute.Value = signingIdentifier.AbsoluteUri;
            signatureMethodElement.Attributes.Append(signatureMethodAlgorithmAttribute);

            signedInfoElement.AppendChild(canonicalizationMethodElement);
            signedInfoElement.AppendChild(signatureMethodElement);

            foreach(var (element, digest, digestAlgorithm, method) in objects)
            {
                var idFromElement = element.GetAttribute("Id");
                var reference = "#" + idFromElement;

                var referenceElement = CreateDSigElement("Reference");
                var referenceUriAttribute = Document.CreateAttribute("URI");
                var referenceTypeAttribute = Document.CreateAttribute("Type");
                referenceUriAttribute.Value = reference;
                referenceTypeAttribute.Value = OpcKnownUris.XmlDSigObject.AbsoluteUri;

                referenceElement.Attributes.Append(referenceUriAttribute);
                referenceElement.Attributes.Append(referenceTypeAttribute);

                var referencesTransformsElement = CreateDSigElement("Transforms");
                var transformElement = CreateDSigElement("Transform");
                var transformAlgorithmAttribute = Document.CreateAttribute("Algorithm");
                transformAlgorithmAttribute.Value = method;
                transformElement.Attributes.Append(transformAlgorithmAttribute);
                referencesTransformsElement.AppendChild(transformElement);
                referenceElement.AppendChild(referencesTransformsElement);

                var digestMethodElement = CreateDSigElement("DigestMethod");
                var digestMethodAlgorithmAttribute = Document.CreateAttribute("Algorithm");
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
            var publicCertificate = Convert.ToBase64String(SigningContext.Certificate.Export(X509ContentType.Cert));
            var keyInfoElement = CreateDSigElement("KeyInfo");
            var x509DataElement = CreateDSigElement("X509Data");
            var x509CertificateElement = CreateDSigElement("X509Certificate");
            x509CertificateElement.InnerText = publicCertificate;
            x509DataElement.AppendChild(x509CertificateElement);
            keyInfoElement.AppendChild(x509DataElement);
            return keyInfoElement;
        }

        public void SetFileManifest(OpcSignatureManifest manifest)
        {
            var objectElement = CreateDSigElement("Object");
            var objectElementId = Document.CreateAttribute("Id");
            objectElementId.Value = "idPackageObject";
            objectElement.Attributes.Append(objectElementId);

            var manifestElement = CreateDSigElement("Manifest");

            foreach (var file in manifest.Manifest)
            {
                var referenceElement = CreateDSigElement("Reference");
                var referenceElementUriAttribute = Document.CreateAttribute("URI");
                referenceElementUriAttribute.Value = file.ReferenceUri.ToQualifiedPath();
                referenceElement.Attributes.Append(referenceElementUriAttribute);

                var digestMethod = CreateDSigElement("DigestMethod");
                var digestMethodAlgorithmAttribute = Document.CreateAttribute("Algorithm");
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
            var signaturePropertyIdAttribute = Document.CreateAttribute("Id");
            var signaturePropertyTargetAttribute = Document.CreateAttribute("Target");
            signaturePropertyIdAttribute.Value = "idSignatureTime";
            signaturePropertyTargetAttribute.Value = "";

            signaturePropertyElement.Attributes.Append(signaturePropertyIdAttribute);
            signaturePropertyElement.Attributes.Append(signaturePropertyTargetAttribute);

            var signatureTimeElement = Document.CreateElement("SignatureTime", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            var signatureTimeFormatElement = Document.CreateElement("Format", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            var signatureTimeValueElement = Document.CreateElement("Value", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            signatureTimeFormatElement.InnerText = "YYYY-MM-DDThh:mm:ss.sTZD";
            signatureTimeValueElement.InnerText = SigningContext.ContextCreationTime.ToString("yyyy-MM-ddTHH:mm:ss.fzzz");

            signatureTimeElement.AppendChild(signatureTimeFormatElement);
            signatureTimeElement.AppendChild(signatureTimeValueElement);

            signaturePropertyElement.AppendChild(signatureTimeElement);
            signaturePropertiesElement.AppendChild(signaturePropertyElement);
            objectElement.AppendChild(signaturePropertiesElement);

            _objectElement = objectElement;
        }
    }
}
