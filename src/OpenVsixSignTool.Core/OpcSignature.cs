using System.Xml.Linq;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// Represents an OPC signature.
    /// </summary>
    /// <remarks>
    /// This type cannot be directly created. To create a signature on a package, use <see cref="OpcPackage.CreateSignatureBuilder". />
    /// </remarks>
    public sealed class OpcSignature
    {
        private readonly OpcPart _signaturePart;

        internal OpcSignature(OpcPart signaturePart)
        {
            _signaturePart = signaturePart;

        }

        public OpcPackageTimestampBuilder CreateTimestampBuilder() => new OpcPackageTimestampBuilder(_signaturePart);
    }
}
