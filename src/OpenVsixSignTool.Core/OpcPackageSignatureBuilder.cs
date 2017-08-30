using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// A builder to sign an OPC package.
    /// </summary>
    public class OpcPackageSignatureBuilder<TEngine> : IOpcPackageSignatureBuilder where TEngine : OpcPackageSignatureEngineBase, new()
    {
        private readonly OpcPackage _package;
        private readonly TEngine _engine;

        internal OpcPackageSignatureBuilder(OpcPackage package)
        {
            _package = package;
            _engine = new TEngine();
        }

        /// <summary>
        /// Creates a signature from the enqueued parts.
        /// </summary>
        /// <param name="configuration">The configuration of properties used to create the signature.
        /// See the documented of <see cref="AzureKeyVaultSignConfigurationSet"/> for more information.</param>
        public async Task<OpcSignature> SignAsync(AzureKeyVaultSignConfigurationSet configuration)
        {
            using (var azureConfiguration = await KeyVaultConfigurationDiscoverer.Materialize(configuration))
            {
                var fileName = azureConfiguration.PublicCertificate.GetCertHashString() + ".psdsxs";
                var signatureFile = SignCore(fileName);
                using (var signingContext = new KeyVaultSigningContext(azureConfiguration))
                {
                    var document = await _engine.SignCore(signingContext, _package);
                    PublishSignature(document, signatureFile);
                }
                _package.Flush();
                return new OpcSignature(signatureFile);
            }
        }

        /// <summary>
        /// Creates a signature from the enqueued parts.
        /// </summary>
        /// <param name="configuration">The configuration of properties used to create the signature.
        /// See the documented of <see cref="CertificateSignConfigurationSet"/> for more information.</param>
        public async Task<OpcSignature> SignAsync(CertificateSignConfigurationSet configuration)
        {
            var fileName = configuration.SigningCertificate.GetCertHashString() + ".psdsxs";
            var signatureFile = SignCore(fileName);
            using (var signingContext = new CertificateSigningContext(configuration.SigningCertificate, configuration.PkcsDigestAlgorithm, configuration.FileDigestAlgorithm))
            {
                var document = await _engine.SignCore(signingContext, _package);
                PublishSignature(document, signatureFile);
            }
            _package.Flush();
            return new OpcSignature(signatureFile);
        }

        private static void PublishSignature(XmlDocument document, OpcPart signatureFile)
        {
            using (var copySignatureStream = signatureFile.Open())
            {
                copySignatureStream.SetLength(0L);
                using (var xmlWriter = new XmlTextWriter(copySignatureStream, System.Text.Encoding.UTF8))
                {
                    //The .NET implementation of OPC used by Visual Studio does not tollerate "white space" nodes.
                    xmlWriter.Formatting = Formatting.None;
                    document.Save(xmlWriter);
                }
            }
        }

        private OpcPart SignCore(string signatureFileName)
        {
            var originFileUri = new Uri("package:///package/services/digital-signature/origin.psdor", UriKind.Absolute);
            var signatureUriRoot = new Uri("package:///package/services/digital-signature/xml-signature/", UriKind.Absolute);
            var originFileRelationship = _package.Relationships.FirstOrDefault(r => r.Type.Equals(OpcKnownUris.DigitalSignatureOrigin));

            OpcPart originFile;
            OpcPart signatureFile;
            //Create the origin file and relationship to the origin file if needed.
            if (originFileRelationship != null)
            {
                originFile = _package.GetPart(originFileRelationship.Target) ?? _package.CreatePart(originFileUri, OpcKnownMimeTypes.DigitalSignatureOrigin);
            }
            else
            {
                originFile = _package.GetPart(originFileUri) ?? _package.CreatePart(originFileUri, OpcKnownMimeTypes.DigitalSignatureOrigin);
                _package.Relationships.Add(new OpcRelationship(originFile.Uri, OpcKnownUris.DigitalSignatureOrigin));
            }

            var signatureRelationship = originFile.Relationships.FirstOrDefault(r => r.Type.Equals(OpcKnownUris.DigitalSignatureSignature));
            if (signatureRelationship != null)
            {
                signatureFile = _package.GetPart(signatureRelationship.Target) ?? _package.CreatePart(originFileUri, OpcKnownMimeTypes.DigitalSignatureSignature);
            }
            else
            {
                var target = new Uri(signatureUriRoot, signatureFileName);
                signatureFile = _package.GetPart(target) ?? _package.CreatePart(target, OpcKnownMimeTypes.DigitalSignatureSignature);
                originFile.Relationships.Add(new OpcRelationship(target, OpcKnownUris.DigitalSignatureSignature));
            }

            _package.Flush();
            return signatureFile;
        }
    }
}
