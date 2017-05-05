using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace OpenVsixSignTool.Core.Tests
{
    public class OpcAzureSigningTests
    {
        private const string SamplePackage = @"sample\OpenVsixSignToolTest.vsix";
        private readonly List<string> _shadowFiles = new List<string>();

        [AzureFact]
        public async Task ShouldSignWithAzureCertificate()
        {
            var creds = TestAzureCredentials.Credentials;
            string path;
            using (var package = ShadowCopyPackage(SamplePackage, out path, OpcPackageFileMode.ReadWrite))
            {
                var builder = package.CreateSignatureBuilder();
                builder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var signature = await builder.SignAsync(
                    new AzureKeyVaultSignConfigurationSet
                    {
                        FileDigestAlgorithm = HashAlgorithmName.SHA256,
                        PkcsDigestAlgorithm = HashAlgorithmName.SHA256,
                        AzureClientId = creds.ClientId,
                        AzureClientSecret = creds.ClientSecret,
                        AzureKeyVaultUrl = creds.AzureKeyVaultUrl,
                        AzureKeyVaultCertificateName = creds.AzureKeyVaultCertificateName
                    }
                );
                Assert.NotNull(signature);
            }
            using (var netfxPackage = Package.Open(path, FileMode.Open))
            {
                var signatureManager = new PackageDigitalSignatureManager(netfxPackage);
                Assert.Equal(VerifyResult.Success, signatureManager.VerifySignatures(true));
                if (signatureManager.Signatures.Count != 1 || signatureManager.Signatures[0].SignedParts.Count != netfxPackage.GetParts().Count() - 1)
                {
                    Assert.True(false, "Missing parts");
                }
                var packageSignature = signatureManager.Signatures[0];
                var expectedAlgorithm = OpcKnownUris.SignatureAlgorithms.rsaSHA256.AbsoluteUri;
                Assert.Equal(expectedAlgorithm, packageSignature.Signature.SignedInfo.SignatureMethod);
            }
        }

        [AzureFact]
        public async Task ShouldSignWithAzureCertificateAndTimestamp()
        {
            var creds = TestAzureCredentials.Credentials;
            string path;
            using (var package = ShadowCopyPackage(SamplePackage, out path, OpcPackageFileMode.ReadWrite))
            {
                var builder = package.CreateSignatureBuilder();
                builder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var signature = await builder.SignAsync(
                    new AzureKeyVaultSignConfigurationSet
                    {
                        FileDigestAlgorithm = HashAlgorithmName.SHA256,
                        PkcsDigestAlgorithm = HashAlgorithmName.SHA256,
                        AzureClientId = creds.ClientId,
                        AzureClientSecret = creds.ClientSecret,
                        AzureKeyVaultUrl = creds.AzureKeyVaultUrl,
                        AzureKeyVaultCertificateName = creds.AzureKeyVaultCertificateName
                    }
                );
                Assert.NotNull(signature);
                var timestampBuilder = signature.CreateTimestampBuilder();
                var timestampServer = new Uri("http://timestamp.digicert.com", UriKind.Absolute);
                var result = await timestampBuilder.SignAsync(timestampServer, HashAlgorithmName.SHA256);
            }
            using (var netfxPackage = Package.Open(path, FileMode.Open))
            {
                var signatureManager = new PackageDigitalSignatureManager(netfxPackage);
                Assert.Equal(VerifyResult.Success, signatureManager.VerifySignatures(true));
                if (signatureManager.Signatures.Count != 1 || signatureManager.Signatures[0].SignedParts.Count != netfxPackage.GetParts().Count() - 1)
                {
                    Assert.True(false, "Missing parts");
                }
                var packageSignature = signatureManager.Signatures[0];
                var expectedAlgorithm = OpcKnownUris.SignatureAlgorithms.rsaSHA256.AbsoluteUri;
                Assert.Equal(expectedAlgorithm, packageSignature.Signature.SignedInfo.SignatureMethod);
            }
        }

        private OpcPackage ShadowCopyPackage(string packagePath, out string path, OpcPackageFileMode mode = OpcPackageFileMode.Read)
        {
            var temp = Path.GetTempFileName();
            _shadowFiles.Add(temp);
            File.Copy(packagePath, temp, true);
            path = temp;
            return OpcPackage.Open(temp, mode);
        }

        public void Dispose()
        {
            void CleanUpShadows()
            {
                _shadowFiles.ForEach(File.Delete);
            }
            CleanUpShadows();
        }
    }
}
