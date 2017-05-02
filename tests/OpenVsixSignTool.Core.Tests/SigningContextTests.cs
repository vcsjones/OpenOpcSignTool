using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace OpenVsixSignTool.Core.Tests
{
    public class SigningContextTests
    {
        [Theory]
        [InlineData(@"certs\rsa-2048-sha256.pfx")]
        [InlineData(@"certs\rsa-2048-sha1.pfx")]
        public void ShouldSignABlobOfDataWithRsaSha256(string pfxPath)
        {
            var certificate = new X509Certificate2(pfxPath, "test");
            using (var context = new CertificateSigningContext(certificate, HashAlgorithmName.SHA256, HashAlgorithmName.SHA256))
            {
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
        }

        [Theory]
        [InlineData(@"certs\rsa-2048-sha256.pfx")]
        [InlineData(@"certs\rsa-2048-sha1.pfx")]
        public void ShouldSignABlobOfDataWithRsaSha1(string pfxPath)
        {
            var certificate = new X509Certificate2(pfxPath, "test");
            using (var context = new CertificateSigningContext(certificate, HashAlgorithmName.SHA1, HashAlgorithmName.SHA1))
            {
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
        }

        [Theory]
        [InlineData(@"certs\ecdsa-p256-sha256.pfx")]
        public void ShouldSignABlobOfDataWithEcdsaP256Sha256(string pfxPath)
        {
            var certificate = new X509Certificate2(pfxPath, "test");
            using (var context = new CertificateSigningContext(certificate, HashAlgorithmName.SHA256, HashAlgorithmName.SHA256))
            {
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
}
