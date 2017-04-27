using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace OpenVsixSignTool.Tests
{
    public class SigningContextTests
    {
        [Fact]
        public void ShouldSignABlobOfDataWithRsa()
        {
            var certificate = new X509Certificate2("sample\\cert.pfx", "test");
            using (var context = new SigningContext(certificate, HashAlgorithmName.SHA256, HashAlgorithmName.SHA1))
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
    }
}
