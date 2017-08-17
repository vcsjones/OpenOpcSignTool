using System.Threading.Tasks;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    public abstract class OpcPackageSignatureEngineBase
    {
        public abstract ISignatureBuilderPreset SigningPreset { get; }
        internal abstract Task<XmlDocument> SignCore(ISigningContext signingContext, OpcSignatureManifest fileManifest);
    }
}
