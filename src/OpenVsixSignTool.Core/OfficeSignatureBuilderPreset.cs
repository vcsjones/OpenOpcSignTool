using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenVsixSignTool.Core
{
    public sealed class OfficeSignatureBuilderPreset : ISignatureBuilderPreset
    {
        private static readonly Uri _trash = new Uri(OpcPackage.BasePackageUri, "[trash]");
        IEnumerable<OpcPart> ISignatureBuilderPreset.GetPartsForSigning(OpcPackage package)
        {
            var existingSignatures = package.GetSignatures().ToList();
            foreach (var part in package.GetParts())
            {
                var isSignaturePart = existingSignatures.Any(existing => part.Uri.EqualOrContainedBy(existing.Part.Uri));
                var isTrash = part.Uri.EqualOrContainedBy(_trash);
                if (!isSignaturePart && !isTrash)
                {
                    yield return part;
                }
            }
        }
    }
}
