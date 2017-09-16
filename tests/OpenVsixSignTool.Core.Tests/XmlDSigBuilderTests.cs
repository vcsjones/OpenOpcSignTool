using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace OpenVsixSignTool.Core.Tests
{
    public class XmlDSigBuilderTests : IDisposable
    {
        private const string SamplePackage = @"sample\OpenVsixSignToolTest.vsix";
        private const string SamplePackageSigned = @"sample\OpenVsixSignToolTest-Signed.vsix";
        private readonly List<string> _shadowFiles = new List<string>();

        [Fact]
        public async Task ShouldGenerateSimpleSignature()
        {
            var certificate = new X509Certificate2(@"certs\rsa-2048-sha256.pfx", "test");
            string path;
            using (var package = ShadowCopyPackage(SamplePackage, out path, OpcPackageFileMode.ReadWrite))
            {
                using (var context = new CertificateSigningContext(certificate, HashAlgorithmName.SHA256, HashAlgorithmName.SHA256))
                {
                    var builder = new XmlDSigBuilder(context);
                    var manifestBuilder = new XmlDSigObjectManifestBuilder();
                    var time = new XmlSignatureTimeSignatureProperty
                    {
                        Value = DateTimeOffset.Now
                    };
                    manifestBuilder.AddSignatureProperty(null, "idSignatureTime", time);
                    foreach(var part in package.GetParts())
                    {
                        manifestBuilder.AddPart(part);
                    }
                    builder.Objects.Add(manifestBuilder);
                    builder.SignedInfo.AddReference(manifestBuilder);
                    var result = await builder.BuildAsync();
                    using (var ms = new MemoryStream())
                    {
                        result.Save(ms);
                        ms.Position = 0;
                        using (var streamReader = new StreamReader(ms))
                        {
                            var contents = streamReader.ReadToEnd();
                            Assert.Equal("", contents);
                        }
                    }
                }
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
