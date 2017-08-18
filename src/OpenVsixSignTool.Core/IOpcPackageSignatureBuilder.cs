using System.Threading.Tasks;

namespace OpenVsixSignTool.Core
{
    public interface IOpcPackageSignatureBuilder
    {
        Task<OpcSignature> SignAsync(AzureKeyVaultSignConfigurationSet configuration);
        Task<OpcSignature> SignAsync(CertificateSignConfigurationSet configuration);
    }
}