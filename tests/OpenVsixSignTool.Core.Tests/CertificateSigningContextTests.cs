using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace OpenVsixSignTool.Core.Tests
{
    public class CertificateSigningContextTests
    {
        private static string CertPath(string str) => Path.Combine("certs", str);

        public static IEnumerable<object[]> RsaCertificates
        {
            get
            {
                yield return new object[] { CertPath("rsa-2048-sha256.pfx") };
                yield return new object[] { CertPath("rsa-2048-sha1.pfx") };
            }
        }

        [Theory]
        [MemberData(nameof(RsaCertificates))]
        public void ShouldSignABlobOfDataWithRsaSha256(string pfxPath)
        {
            var certificate = new X509Certificate2(pfxPath, "test");
            var config = new SignConfigurationSet
            (
                publicCertificate: certificate,
                signatureDigestAlgorithm: HashAlgorithmName.SHA256,
                fileDigestAlgorithm: HashAlgorithmName.SHA256,
                signingKey: certificate.GetRSAPrivateKey()
            );

            var context = new SigningContext(config);
            using (var hash = SHA256.Create())
            {
                var digest = hash.ComputeHash(new byte[] { 1, 2, 3 });
                var signature = context.SignDigest(digest);
                Assert.Equal(OpcKnownUris.SignatureAlgorithms.rsaSHA256, context.XmlDSigIdentifier);
                Assert.Equal(SigningAlgorithm.RSA, context.SignatureAlgorithm);

                var roundtrips = context.VerifyDigest(digest, signature);
                Assert.True(roundtrips);
            }
        }

        [Theory]
        [MemberData(nameof(RsaCertificates))]
        public void ShouldSignABlobOfDataWithRsaSha1(string pfxPath)
        {
            var certificate = new X509Certificate2(pfxPath, "test");
            var config = new SignConfigurationSet
            (
                publicCertificate: certificate,
                signatureDigestAlgorithm: HashAlgorithmName.SHA1,
                fileDigestAlgorithm: HashAlgorithmName.SHA1,
                signingKey: certificate.GetRSAPrivateKey()
            );

            var context = new SigningContext(config);
            using (var hash = SHA1.Create())
            {
                var digest = hash.ComputeHash(new byte[] { 1, 2, 3 });
                var signature = context.SignDigest(digest);
                Assert.Equal(OpcKnownUris.SignatureAlgorithms.rsaSHA1, context.XmlDSigIdentifier);
                Assert.Equal(SigningAlgorithm.RSA, context.SignatureAlgorithm);

                var roundtrips = context.VerifyDigest(digest, signature);
                Assert.True(roundtrips);
            }
        }

        [Fact]
        public void ShouldSignABlobOfDataWithEcdsaP256Sha256()
        {
            var certificate = new X509Certificate2(CertPath("ecdsa-p256-sha256.pfx"), "test");
            var config = new SignConfigurationSet
            (
                publicCertificate: certificate,
                signatureDigestAlgorithm: HashAlgorithmName.SHA256,
                fileDigestAlgorithm: HashAlgorithmName.SHA256,
                signingKey: certificate.GetECDsaPrivateKey()
            );

            var context = new SigningContext(config);
            using (var hash = SHA256.Create())
            {
                var digest = hash.ComputeHash(new byte[] { 1, 2, 3 });
                var signature = context.SignDigest(digest);
                Assert.Equal(OpcKnownUris.SignatureAlgorithms.ecdsaSHA256, context.XmlDSigIdentifier);
                Assert.Equal(SigningAlgorithm.ECDSA, context.SignatureAlgorithm);

                var roundtrips = context.VerifyDigest(digest, signature);
                Assert.True(roundtrips);
            }
        }
    }
}
