using System;
using System.Collections.Generic;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// The preset for VSIX files.
    /// </summary>
    public sealed class VSIXSignatureBuilderPreset : ISignatureBuilderPreset
    {
        IEnumerable<OpcPart> ISignatureBuilderPreset.GetPartsForSigning(OpcPackage package)
        {
            var signaturePart = package.GetSignaturePart();
            foreach (var part in package.GetParts())
            {
                //We don't want to sign an existing signature.
                if (signaturePart != null && Uri.Compare(part.Uri, signaturePart.Uri, UriComponents.Path, UriFormat.Unescaped, StringComparison.Ordinal) == 0)
                {
                    continue;
                }
                yield return part;
            }
        }
    }
}
