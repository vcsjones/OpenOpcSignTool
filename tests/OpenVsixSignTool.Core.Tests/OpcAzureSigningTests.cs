using System;
using System.Collections.Generic;
using System.IO;
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
            using (var netfxPackage = OpcPackage.Open(path))
            {
                Assert.NotEmpty(netfxPackage.GetSignatures());
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

            using (var netfxPackage = OpcPackage.Open(path))
            {
                Assert.NotEmpty(netfxPackage.GetSignatures());
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
