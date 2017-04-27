using System.Collections.Generic;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// The preset for VSIX files.
    /// </summary>
    public sealed class VSIXSignatureBuilderPreset : ISignatureBuilderPreset
    {
        IEnumerable<OpcPart> ISignatureBuilderPreset.GetPartsForSigning(OpcPackage package) => package.GetParts();
    }
}
