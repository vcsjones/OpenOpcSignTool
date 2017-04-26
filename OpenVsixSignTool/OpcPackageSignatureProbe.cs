using System;
using System.Linq;

namespace OpenVsixSignTool
{
    internal static class OpcPackageSignatureProbe
    {

        /// <summary>
        /// Gets the signature part, or null if not present.
        /// </summary>
        /// <param name="package">The package to probe for a signature.</param>
        /// <returns>The signature part, or null if not present.</returns>
        public static OpcPart GetSignaturePart(this OpcPackage package)
        {
            var originFileUri = new Uri("package:///package/services/digital-signature/origin.psdor", UriKind.Absolute);
            var originFileRelationship = package.Relationships.FirstOrDefault(r => r.Type.Equals(OpcKnownUris.DigitalSignatureOrigin));
            if (originFileRelationship == null)
            {
                return null;
            }
            var originPart = package.GetPart(originFileRelationship.Target);
            var signatureRelationship = originPart?.Relationships.FirstOrDefault(r => r.Type.Equals(OpcKnownUris.DigitalSignatureSignature));
            if (signatureRelationship == null)
            {
                return null;
            }
            return package.GetPart(signatureRelationship.Target);
        }
    }
}
