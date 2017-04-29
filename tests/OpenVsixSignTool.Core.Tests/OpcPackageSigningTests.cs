using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace OpenVsixSignTool.Core.Tests
{
    public class OpcPackageSigningTests : IDisposable
    {
        private const string SamplePackage = @"sample\OpenVsixSignToolTest.vsix";
        private const string SamplePackageSigned = @"sample\OpenVsixSignToolTest-Signed.vsix";
        private readonly List<string> _shadowFiles = new List<string>();


        [Theory]
        [MemberData(nameof(RsaSigningTheories))]
        public void ShouldSignFileWithRsa(string pfxPath, HashAlgorithmName fileDigestAlgorithm, string expectedAlgorithm)
        {
            string path;
            using (var package = ShadowCopyPackage(SamplePackage, out path, OpcPackageFileMode.ReadWrite))
            {
                var builder = package.CreateSignatureBuilder();
                builder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                builder.Sign(fileDigestAlgorithm, new X509Certificate2(pfxPath, "test"));
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
                Assert.Equal(expectedAlgorithm, packageSignature.Signature.SignedInfo.SignatureMethod);
            }
        }

        public static IEnumerable<object[]> RsaSigningTheories
        {
            get
            {
                yield return new object[] { @"certs\rsa-2048-sha256.pfx", HashAlgorithmName.SHA512, OpcKnownUris.SignatureAlgorithms.rsaSHA512.AbsoluteUri };
                yield return new object[] { @"certs\rsa-2048-sha256.pfx", HashAlgorithmName.SHA384, OpcKnownUris.SignatureAlgorithms.rsaSHA384.AbsoluteUri };
                yield return new object[] { @"certs\rsa-2048-sha256.pfx", HashAlgorithmName.SHA256, OpcKnownUris.SignatureAlgorithms.rsaSHA256.AbsoluteUri };
                yield return new object[] { @"certs\rsa-2048-sha256.pfx", HashAlgorithmName.SHA1, OpcKnownUris.SignatureAlgorithms.rsaSHA1.AbsoluteUri };
                yield return new object[] { @"certs\rsa-2048-sha1.pfx", HashAlgorithmName.SHA512, OpcKnownUris.SignatureAlgorithms.rsaSHA512.AbsoluteUri };
                yield return new object[] { @"certs\rsa-2048-sha1.pfx", HashAlgorithmName.SHA384, OpcKnownUris.SignatureAlgorithms.rsaSHA384.AbsoluteUri };
                yield return new object[] { @"certs\rsa-2048-sha1.pfx", HashAlgorithmName.SHA256, OpcKnownUris.SignatureAlgorithms.rsaSHA256.AbsoluteUri };
                yield return new object[] { @"certs\rsa-2048-sha1.pfx", HashAlgorithmName.SHA1, OpcKnownUris.SignatureAlgorithms.rsaSHA1.AbsoluteUri };
            }
        }

        [Theory]
        [MemberData(nameof(RsaTimestampTheories))]
        public void ShouldTimestampFileWithRsa(string pfxPath, HashAlgorithmName timestampDigestAlgorithm)
        {
            using (var package = ShadowCopyPackage(SamplePackage, out _, OpcPackageFileMode.ReadWrite))
            {
                var signerBuilder = package.CreateSignatureBuilder();
                signerBuilder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var signature = signerBuilder.Sign(HashAlgorithmName.SHA256, new X509Certificate2(pfxPath, "test"));
                var timestampBuilder = signature.CreateTimestampBuilder();
                var result = timestampBuilder.Sign(new Uri("http://timestamp.digicert.com"), timestampDigestAlgorithm);
                Assert.Equal(TimestampResult.Success, result);
            }
        }

        public static IEnumerable<object[]> RsaTimestampTheories
        {
            get
            {
                yield return new object[] { @"certs\rsa-2048-sha256.pfx", HashAlgorithmName.SHA256 };
                yield return new object[] { @"certs\rsa-2048-sha256.pfx", HashAlgorithmName.SHA1 };
                yield return new object[] { @"certs\rsa-2048-sha1.pfx", HashAlgorithmName.SHA256 };
                yield return new object[] { @"certs\rsa-2048-sha1.pfx", HashAlgorithmName.SHA1 };
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
