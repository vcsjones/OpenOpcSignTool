using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    public sealed class VSIXPackageSignatureEngine : OpcPackageSignatureEngineBase
    {
        internal override async Task<XmlDocument> SignCore(ISigningContext signingContext, OpcPackage package)
        {
            var builder = new XmlDSigBuilder(signingContext);
            var manifest = new XmlDSigObjectManifestBuilder();
            foreach(var part in GetPartsForSigning(package))
            {
                manifest.AddPart(part);
            }
            return await builder.BuildAsync();
        }

        private static IEnumerable<OpcPart> GetPartsForSigning(OpcPackage package)
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

    public sealed class OfficePackageSignatureEngine : OpcPackageSignatureEngineBase
    {
        internal override async Task<XmlDocument> SignCore(ISigningContext signingContext, OpcPackage package)
        {
            var builder = new XmlDSigBuilder(signingContext);
            var objects = new XmlDSigObjectManifestBuilder();
            return await builder.BuildAsync();
        }
    }

}
