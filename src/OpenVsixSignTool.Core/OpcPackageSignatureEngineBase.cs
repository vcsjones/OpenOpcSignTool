using System.Threading.Tasks;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    public abstract class OpcPackageSignatureEngineBase
    {
        internal abstract Task<XmlDocument> SignCore(ISigningContext signingContext, OpcPackage package);
    }
}
