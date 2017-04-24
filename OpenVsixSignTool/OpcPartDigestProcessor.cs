using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool
{
    internal class OpcPartDigestProcessor
    {
        //These are documented here. https://www.iana.org/assignments/xml-security-uris/xml-security-uris.xhtml
        private static readonly Uri _md5DigestUri = new Uri("http://www.w3.org/2001/04/xmldsig-more#md5");
        private static readonly Uri _sha1DigestUri = new Uri("http://www.w3.org/2000/09/xmldsig#sha1");
        private static readonly Uri _sha224DigestUri = new Uri("http://www.w3.org/2001/04/xmldsig-more#sha224");
        private static readonly Uri _sha256DigestUri = new Uri("http://www.w3.org/2001/04/xmlenc#sha256");
        private static readonly Uri _sha384DigestUri = new Uri("http://www.w3.org/2001/04/xmldsig-more#sha384");
        private static readonly Uri _sha512DigestUri = new Uri("http://www.w3.org/2001/04/xmlenc#sha512");

        public static (byte[] digest, Uri identifier) Digest(OpcPart part, HashAlgorithmName algorithmName)
        {
            using (var hashAlgorithm = NameToAlgorithm(algorithmName, out var identifier))
            {
                var digest = hashAlgorithm.ComputeHash(part.Open());
                return (digest, identifier);
            }
        }

        private static HashAlgorithm NameToAlgorithm(HashAlgorithmName algorithmName, out Uri identifier)
        {
            if (algorithmName == HashAlgorithmName.MD5)
            { 
                identifier = _md5DigestUri;
                return MD5.Create();
            }
            else if (algorithmName == HashAlgorithmName.SHA1)
            {
                identifier = _sha1DigestUri;
                return SHA1.Create();
            }
            else if (algorithmName == HashAlgorithmName.SHA256)
            {
                identifier = _sha256DigestUri;
                return SHA256.Create();
            }
            else if (algorithmName == HashAlgorithmName.SHA384)
            {
                identifier = _sha384DigestUri;
                return SHA384.Create();
            }

            else if (algorithmName == HashAlgorithmName.SHA512)
            {
                identifier = _sha512DigestUri;
                return SHA512.Create();
            }
            else
            {
                throw new NotSupportedException("The algorithm selected is not supported.");
            }
        }
    }

}
