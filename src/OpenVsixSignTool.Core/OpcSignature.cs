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

        /// <summary>
        /// Gets the part in the package for this signatures.
        /// </summary>
        public OpcPart Part => _signaturePart;

        /// <summary>
        /// Creates a builder to timestamp the existing signature.
        /// </summary>
        /// <returns>An <see cref="OpcPackageTimestampBuilder"/> that allows building and configuring timestamps.</returns>
        public OpcPackageTimestampBuilder CreateTimestampBuilder() => new OpcPackageTimestampBuilder(_signaturePart);
    }
}
