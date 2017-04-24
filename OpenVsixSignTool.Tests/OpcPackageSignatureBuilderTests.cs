using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace OpenVsixSignTool.Tests
{
    public class OpcPackageSignatureBuilderTests
    {
        [Fact]
        public void Garbageest()
        {

            var transformer = new XmlDsigC14NTransform(false);
            var sig = System.Convert.FromBase64String("hDgS66wdCL8Hv8o9QfC5mu8ISzHBmqUBEGuBe/HDJfN8ekvEg3ynrISY1V/u+OgT8NctawBFcaqsN6fficI8fhdSwdtZ9LL1gLWP3oA7QkPiafCdgJWJoWS2whNRFC8QSThruGVNyf/zL3o+kbm8JfmStmJHHZRBKIeue3LQXk6/C0K9bZGYs8+ou5UXAbYHaRK4kXwc3vxNv2CY8ZUnmiIr+GMHaTgxN4Jn8dE9WvaszzoNSN9L1MyCbgJy2Sss/GCo0sGZ7VeqdX2p3+Xec3LahUheG6nn1ZZgDIPjy5zeJQUiHFcVNc2GnvhQECc+GrN6spjjWxEpua21p7IxFw==");
            var doc = new XmlDocument();
            doc.Load("sample\\sign.xml");

            transformer.LoadInput(doc);
            var output = (Stream)transformer.GetOutput();
            var digest = SHA256.Create().ComputeHash(output);

            var certificate = new X509Certificate2("sample\\cert.pfx", "test");
            var result = certificate.GetRSAPublicKey().VerifyHash(digest, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        }
    }
}
