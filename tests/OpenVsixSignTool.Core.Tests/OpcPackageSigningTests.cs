﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace OpenVsixSignTool.Core.Tests
{
    public class OpcPackageSigningTests : IDisposable
    {
        private static readonly string SamplePackage = Path.Combine("sample" , "OpenVsixSignToolTest.vsix");
        private static readonly string SamplePackageSigned = Path.Combine("sample", "OpenVsixSignToolTest-Signed.vsix");
        private readonly List<string> _shadowFiles = new List<string>();

        private static string CertPath(string str) => Path.Combine("certs", str);


        [Theory]
        [MemberData(nameof(RsaSigningTheories))]
        public void ShouldSignFileWithRsa(string pfxPath, HashAlgorithmName fileDigestAlgorithm, string expectedAlgorithm)
        {
            _ = expectedAlgorithm;

            using (var package = ShadowCopyPackage(SamplePackage, out string path, OpcPackageFileMode.ReadWrite))
            {
                var builder = package.CreateSignatureBuilder();
                builder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var certificate = new X509Certificate2(pfxPath, "test");
                var result = builder.Sign(
                 new SignConfigurationSet(
                    publicCertificate: certificate,
                    signatureDigestAlgorithm: fileDigestAlgorithm,
                    fileDigestAlgorithm: fileDigestAlgorithm,
                    signingKey: certificate.GetRSAPrivateKey()
                ));
                Assert.NotNull(result);
            }
        }

        [Theory]
        [MemberData(nameof(EcdsaSigningTheories))]
        public void ShouldSignFileWithEcdsa(string pfxPath, HashAlgorithmName fileDigestAlgorithm, string expectedAlgorithm)
        {
            _ = expectedAlgorithm;

            using (var package = ShadowCopyPackage(SamplePackage, out string path, OpcPackageFileMode.ReadWrite))
            {
                var builder = package.CreateSignatureBuilder();
                builder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var certificate = new X509Certificate2(pfxPath, "test");
                var result = builder.Sign(
                 new SignConfigurationSet(
                    publicCertificate: certificate,
                    signatureDigestAlgorithm: fileDigestAlgorithm,
                    fileDigestAlgorithm: fileDigestAlgorithm,
                    signingKey: certificate.GetECDsaPrivateKey()
                ));
            }
        }

        public static IEnumerable<object[]> RsaSigningTheories
        {
            get
            {
                yield return new object[] { CertPath("rsa-2048-sha256.pfx"), HashAlgorithmName.SHA512, OpcKnownUris.SignatureAlgorithms.rsaSHA512.AbsoluteUri };
                yield return new object[] { CertPath("rsa-2048-sha256.pfx"), HashAlgorithmName.SHA384, OpcKnownUris.SignatureAlgorithms.rsaSHA384.AbsoluteUri };
                yield return new object[] { CertPath("rsa-2048-sha256.pfx"), HashAlgorithmName.SHA256, OpcKnownUris.SignatureAlgorithms.rsaSHA256.AbsoluteUri };
                yield return new object[] { CertPath("rsa-2048-sha256.pfx"), HashAlgorithmName.SHA1, OpcKnownUris.SignatureAlgorithms.rsaSHA1.AbsoluteUri };
                yield return new object[] { CertPath("rsa-2048-sha1.pfx"), HashAlgorithmName.SHA512, OpcKnownUris.SignatureAlgorithms.rsaSHA512.AbsoluteUri };
                yield return new object[] { CertPath("rsa-2048-sha1.pfx"), HashAlgorithmName.SHA384, OpcKnownUris.SignatureAlgorithms.rsaSHA384.AbsoluteUri };
                yield return new object[] { CertPath("rsa-2048-sha1.pfx"), HashAlgorithmName.SHA256, OpcKnownUris.SignatureAlgorithms.rsaSHA256.AbsoluteUri };
                yield return new object[] { CertPath("rsa-2048-sha1.pfx"), HashAlgorithmName.SHA1, OpcKnownUris.SignatureAlgorithms.rsaSHA1.AbsoluteUri };
            }
        }

        public static IEnumerable<object[]> EcdsaSigningTheories
        {
            get
            {
                yield return new object[] { CertPath("ecdsa-p256-sha256.pfx"), HashAlgorithmName.SHA256, OpcKnownUris.SignatureAlgorithms.ecdsaSHA256.AbsoluteUri };
                yield return new object[] { CertPath("ecdsa-p256-sha256.pfx"), HashAlgorithmName.SHA1, OpcKnownUris.SignatureAlgorithms.ecdsaSHA1.AbsoluteUri };
            }
        }

        [Theory]
        [MemberData(nameof(RsaTimestampTheories))]
        public async Task ShouldTimestampFileWithRsa(string pfxPath, HashAlgorithmName timestampDigestAlgorithm)
        {
            using (var package = ShadowCopyPackage(SamplePackage, out var path, OpcPackageFileMode.ReadWrite))
            {
                var signerBuilder = package.CreateSignatureBuilder();
                signerBuilder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var certificate = new X509Certificate2(pfxPath, "test");
                var signature = signerBuilder.Sign(
                 new SignConfigurationSet(
                    publicCertificate: certificate,
                    signatureDigestAlgorithm: HashAlgorithmName.SHA256,
                    fileDigestAlgorithm: HashAlgorithmName.SHA256,
                    signingKey: certificate.GetRSAPrivateKey()
                ));
                var timestampBuilder = signature.CreateTimestampBuilder();
                var result = await timestampBuilder.SignAsync(new Uri("http://timestamp.digicert.com"), timestampDigestAlgorithm);
                Assert.Equal(TimestampResult.Success, result);
            }
        }

        [Fact]
        public void ShouldSupportReSigning()
        {
            string path;
            var certificate = new X509Certificate2(CertPath("rsa-2048-sha256.pfx"), "test");
            using (var package = ShadowCopyPackage(SamplePackage, out path, OpcPackageFileMode.ReadWrite))
            {
                var signerBuilder = package.CreateSignatureBuilder();
                signerBuilder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                signerBuilder.Sign(
                 new SignConfigurationSet(
                    publicCertificate: certificate,
                    signatureDigestAlgorithm: HashAlgorithmName.SHA256,
                    fileDigestAlgorithm: HashAlgorithmName.SHA256,
                    signingKey: certificate.GetRSAPrivateKey()
                ));
            }
            using (var package = OpcPackage.Open(path, OpcPackageFileMode.ReadWrite))
            {
                var signerBuilder = package.CreateSignatureBuilder();
                signerBuilder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                signerBuilder.Sign(
                 new SignConfigurationSet(
                    publicCertificate: certificate,
                    signatureDigestAlgorithm: HashAlgorithmName.SHA256,
                    fileDigestAlgorithm: HashAlgorithmName.SHA256,
                    signingKey: certificate.GetRSAPrivateKey()
                ));
            }
            using (var netfxPackage = OpcPackage.Open(path))
            {
                Assert.NotEmpty(netfxPackage.GetSignatures());
            }
        }

        [Fact]
        public void ShouldSupportReSigningWithDifferentCertificate()
        {
            string path;
            using (var package = ShadowCopyPackage(SamplePackage, out path, OpcPackageFileMode.ReadWrite))
            {
                var signerBuilder = package.CreateSignatureBuilder();
                signerBuilder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var rsaSha1Cert = new X509Certificate2(CertPath("rsa-2048-sha1.pfx"), "test");
                signerBuilder.Sign(
                 new SignConfigurationSet(
                    publicCertificate: rsaSha1Cert,
                    signatureDigestAlgorithm: HashAlgorithmName.SHA256,
                    fileDigestAlgorithm: HashAlgorithmName.SHA256,
                    signingKey: rsaSha1Cert.GetRSAPrivateKey()
                ));
            }
            using (var package = OpcPackage.Open(path, OpcPackageFileMode.ReadWrite))
            {
                var signerBuilder = package.CreateSignatureBuilder();
                signerBuilder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var rsaSha256Cert = new X509Certificate2(CertPath("rsa-2048-sha256.pfx"), "test");
                signerBuilder.Sign(
                 new SignConfigurationSet(
                    publicCertificate: rsaSha256Cert,
                    signatureDigestAlgorithm: HashAlgorithmName.SHA256,
                    fileDigestAlgorithm: HashAlgorithmName.SHA256,
                    signingKey: rsaSha256Cert.GetRSAPrivateKey()
                ));
            }
            using (var netfxPackage = OpcPackage.Open(path))
            {
                Assert.NotEmpty(netfxPackage.GetSignatures());
            }
        }

        [Fact]
        public void ShouldRemoveSignature()
        {
            string path;
            using (var package = ShadowCopyPackage(SamplePackage, out path, OpcPackageFileMode.ReadWrite))
            {
                var certificate = new X509Certificate2(CertPath("rsa-2048-sha1.pfx"), "test");
                var signerBuilder = package.CreateSignatureBuilder();
                signerBuilder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                signerBuilder.Sign(
                 new SignConfigurationSet(
                    publicCertificate: certificate,
                    signatureDigestAlgorithm: HashAlgorithmName.SHA1,
                    fileDigestAlgorithm: HashAlgorithmName.SHA1,
                    signingKey: certificate.GetRSAPrivateKey()
                ));
            }
            using (var package = OpcPackage.Open(path, OpcPackageFileMode.ReadWrite))
            {
                var signatures = package.GetSignatures().ToList();
                Assert.Single(signatures);
                var signature = signatures[0];
                signature.Remove();
                Assert.Null(signature.Part);
                Assert.Throws<InvalidOperationException>(() => signature.CreateTimestampBuilder());
                Assert.Empty(package.GetSignatures());
            }
        }

        public static IEnumerable<object[]> RsaTimestampTheories
        {
            get
            {
                yield return new object[] { CertPath("rsa-2048-sha256.pfx"), HashAlgorithmName.SHA256 };
                yield return new object[] { CertPath("rsa-2048-sha256.pfx"), HashAlgorithmName.SHA1 };
                yield return new object[] { CertPath("rsa-2048-sha1.pfx"), HashAlgorithmName.SHA256 };
                yield return new object[] { CertPath("rsa-2048-sha1.pfx"), HashAlgorithmName.SHA1 };
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
