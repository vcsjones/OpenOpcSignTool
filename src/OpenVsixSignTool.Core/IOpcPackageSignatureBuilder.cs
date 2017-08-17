using System.Threading.Tasks;

namespace OpenVsixSignTool.Core
{
    public interface IOpcPackageSignatureBuilder
    {
        bool DequeuePart(OpcPart part);
        void EnqueueEngineDefaults();
        void EnqueuePart(OpcPart part);
        Task<OpcSignature> SignAsync(AzureKeyVaultSignConfigurationSet configuration);
        Task<OpcSignature> SignAsync(CertificateSignConfigurationSet configuration);
    }
}