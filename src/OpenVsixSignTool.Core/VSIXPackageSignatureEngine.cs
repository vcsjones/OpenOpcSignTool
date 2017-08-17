using System.Threading.Tasks;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    public sealed class VSIXPackageSignatureEngine : OpcPackageSignatureEngineBase
    {
        public override ISignatureBuilderPreset SigningPreset { get; } = new VSIXSignatureBuilderPreset();

        internal override async Task<XmlDocument> SignCore(ISigningContext signingContext, OpcSignatureManifest fileManifest)
        {
            var builder = new XmlSignatureBuilder(signingContext);
            builder.SetFileManifest(fileManifest);
            return await builder.BuildAsync();
        }
    }
}
