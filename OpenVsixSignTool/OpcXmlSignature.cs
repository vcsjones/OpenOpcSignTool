using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace OpenVsixSignTool
{
    internal class OpcXmlSignature
    {
        private static Uri BarePackageUri = new Uri("package:", UriKind.Absolute);

        public OpcXmlSignature(OpcSignatureManifest fileManifest, X509Certificate2 signingCertificate)
        {
            var document = new XmlDocument();
            var manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("", OpcKnownUris.XmlDSig.AbsoluteUri);
            var signatureElement = document.CreateElement("Signature");

            var keyInfoElement = CreateKeyInfoElement(document, signingCertificate);
            var objectElement = CreateObjectElement(document, fileManifest);

            var signedInfoElement = CreateSignedInfo(document, objectElement);

            signatureElement.AppendChild(signedInfoElement);
            signatureElement.AppendChild(keyInfoElement);
            signatureElement.AppendChild(objectElement);
            document.AppendChild(signatureElement);
        }

        private static (byte[] digest, string canonicalizationMethod) CanonicalizeSignedParts(XmlDocument document, XmlElement objectElement)
        {
            var transformer = new XmlDsigC14NTransform(false);

            var canonicalizationDocument = new XmlDocument(document.NameTable);
            canonicalizationDocument.LoadXml(objectElement.OuterXml);
            transformer.LoadInput(canonicalizationDocument);
            var output = (Stream)transformer.GetOutput();
            var digest = SHA256.Create().ComputeHash(output);
            return (digest, transformer.Algorithm);
        }

        private static XmlElement CreateSignedInfo(XmlDocument document, params XmlElement[] references)
        {
            var transformer = new XmlDsigC14NTransform(false);

            var signedInfoElement = document.CreateElement("SignedInfo");
            var canonicalizationMethodElement = document.CreateElement("CanonicalizationMethod");
            var canonicalizationMethodAlgorithmAttribute = document.CreateAttribute("Algorithm");
            canonicalizationMethodAlgorithmAttribute.Value = "TODO";
            canonicalizationMethodElement.Attributes.Append(canonicalizationMethodAlgorithmAttribute);

            var signatureMethodElement = document.CreateElement("SignatureMethod");
            var signatureMethodAlgorithmAttribute = document.CreateAttribute("Attribute");
            signatureMethodAlgorithmAttribute.Value = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            signatureMethodElement.Attributes.Append(signatureMethodAlgorithmAttribute);

            signedInfoElement.AppendChild(canonicalizationMethodElement);
            signedInfoElement.AppendChild(signatureMethodElement);

            return signedInfoElement;
        }

        private static XmlElement CreateReference(XmlDocument document, XmlElement element)
        {
            var canonicalizedPart = CanonicalizeSignedParts(document, element);
            return null;

        }

        private static XmlElement CreateKeyInfoElement(XmlDocument document, X509Certificate2 certificate)
        {
            var publicCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
            var keyInfoElement = document.CreateElement("KeyInfo");
            var x509DataElement = document.CreateElement("X509Data");
            var x509CertificateElement = document.CreateElement("X509Certificate");
            x509CertificateElement.InnerText = publicCertificate;
            x509DataElement.AppendChild(x509CertificateElement);
            keyInfoElement.AppendChild(x509DataElement);
            return keyInfoElement;
        }

        private static XmlElement CreateObjectElement(XmlDocument document, OpcSignatureManifest fileManifest)
        {
            var objectElement = document.CreateElement("Object");
            var objectElementId = document.CreateAttribute("Id");
            objectElementId.Value = "idPackageObject";
            objectElement.Attributes.Append(objectElementId);

            var manifestElement = document.CreateElement("Manifest");

            foreach(var file in fileManifest.Manifest)
            {
                var referenceElement = document.CreateElement("Reference");
                var referenceElementUriAttribute = document.CreateAttribute("URI");
                referenceElementUriAttribute.Value = GetEntryNameFromUri(file.ReferenceUri);
                referenceElement.Attributes.Append(referenceElementUriAttribute);

                var digestMethod = document.CreateElement("DigestMethod");
                var digestMethodAlgorithmAttribute = document.CreateAttribute("Algorithm");
                digestMethodAlgorithmAttribute.Value = file.DigestAlgorithmIdentifier.AbsoluteUri;
                digestMethod.Attributes.Append(digestMethodAlgorithmAttribute);
                referenceElement.AppendChild(digestMethod);

                var digestValue = document.CreateElement("DigestValue");
                digestValue.InnerText = System.Convert.ToBase64String(file.Digest);
                referenceElement.AppendChild(digestValue);


                manifestElement.AppendChild(referenceElement);
            }

            objectElement.AppendChild(manifestElement);

            var signaturePropertiesElement = document.CreateElement("SignatureProperties");
            var signaturePropertyElement = document.CreateElement("SignatureProperty");
            var signaturePropertyIdAttribute = document.CreateAttribute("Id");
            var signaturePropertyTargetAttribute = document.CreateAttribute("Target");
            signaturePropertyIdAttribute.Value = "idSignatureTime";
            signaturePropertyTargetAttribute.Value = "";

            signaturePropertyElement.Attributes.Append(signaturePropertyIdAttribute);
            signaturePropertyElement.Attributes.Append(signaturePropertyTargetAttribute);

            var signatureTimeElement = document.CreateElement("SignatureTime", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            var signatureTimeFormatElement = document.CreateElement("Format", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            var signatureTimeValueElement = document.CreateElement("Value", OpcKnownUris.XmlDigitalSignature.AbsoluteUri);
            signatureTimeFormatElement.InnerText = "YYYY-MM-DDThh:mm:ss.sTZD";
            signatureTimeValueElement.InnerText = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fzzz");

            signatureTimeElement.AppendChild(signatureTimeFormatElement);
            signatureTimeElement.AppendChild(signatureTimeValueElement);

            signaturePropertyElement.AppendChild(signatureTimeElement);
            signaturePropertiesElement.AppendChild(signaturePropertyElement);
            objectElement.AppendChild(signaturePropertiesElement);
            return objectElement;
        }

        private static string GetEntryNameFromUri(Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(OpcPackage.BasePackageUri, partUri);
            var resolved = BarePackageUri.MakeRelativeUri(absolute);
            return resolved.ToString();
        }
    }
}
