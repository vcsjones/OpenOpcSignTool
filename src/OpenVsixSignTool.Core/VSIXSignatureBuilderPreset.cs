using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// The preset for VSIX files.
    /// </summary>
    public sealed class VSIXSignatureBuilderPreset : ISignatureBuilderPreset
    {
        IEnumerable<OpcPart> ISignatureBuilderPreset.GetPartsForSigning(OpcPackage package)
        {
            var existingSignatures = package.GetSignatures().ToList();
            foreach (var part in package.GetParts())
            {
                if (existingSignatures.All(existing => Uri.Compare(part.Uri, existing.Part.Uri, UriComponents.Path, UriFormat.Unescaped, StringComparison.Ordinal) != 0))
                {
                    yield return part;
                }
            }
        }
    }
}
