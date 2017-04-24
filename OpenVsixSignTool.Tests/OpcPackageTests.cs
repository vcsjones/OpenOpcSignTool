using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace OpenVsixSignTool.Tests
{
    public class OpcPackageTests : IDisposable
    {
        private readonly List<string> _shadowFiles = new List<string>();

        [Fact]
        public void ShouldOpenAndDisposeAPackageAndDisposeIsIdempotent()
        {
            var package = OpcPackage.Open(@"sample\VsVim.vsix");
            package.Dispose();
            package.Dispose();
        }

        [Fact]
        public void ShouldReadContentTypes()
        {
            using (var package = OpcPackage.Open(@"sample\VsVim.vsix"))
            {
                Assert.Equal(7, package.ContentTypes.Count);
                var first = package.ContentTypes[0];
                Assert.Equal("vsixmanifest", first.Extension);
                Assert.Equal("text/xml", first.ContentType);
                Assert.Equal(OpcContentTypeMode.Default, first.Mode);
            }
        }

        [Fact]
        public void ShouldNotAllowUpdatingContentTypesInReadOnly()
        {
            using (var package = OpcPackage.Open(@"sample\VsVim.vsix"))
            {
                var newItem = new OpcContentType("test", "test", OpcContentTypeMode.Override);
                var contentTypes = package.ContentTypes;
                Assert.Throws<InvalidOperationException>(() => contentTypes.Add(newItem));
            }
        }

        [Fact]
        public void ShouldAllowUpdatingContentType()
        {
            int initialCount;
            string shadowPath;
            using (var package = ShadowCopyPackage(@"sample\VsVim.vsix", out shadowPath, OpcPackageFileMode.ReadWrite))
            {
                initialCount = package.ContentTypes.Count;
                var newItem = new OpcContentType("test", "application/test", OpcContentTypeMode.Override);
                package.ContentTypes.Add(newItem);
                package.Flush();
            }
            using (var reopenedPackage = OpcPackage.Open(shadowPath))
            {
                Assert.Equal(initialCount + 1, reopenedPackage.ContentTypes.Count);
            }
        }

        [Fact]
        public void ShouldAllowUpdatingRelationships()
        {
            int initialCount;
            string shadowPath;
            using (var package = ShadowCopyPackage(@"sample\VsVim.vsix", out shadowPath, OpcPackageFileMode.ReadWrite))
            {
                initialCount = package.Relationships.Count;
                var newItem = new OpcRelationship(new Uri("/test", UriKind.RelativeOrAbsolute), new Uri("/test", UriKind.RelativeOrAbsolute));
                package.Relationships.Add(newItem);
                Assert.True(newItem.Id != null && newItem.Id.Length == 9);
                package.Flush();
            }
            using (var reopenedPackage = OpcPackage.Open(shadowPath))
            {
                Assert.Equal(initialCount + 1, reopenedPackage.Relationships.Count);
            }
        }

        [Fact]
        public void ShouldEnumerateAllParts()
        {
            using (var package = OpcPackage.Open(@"sample\VsVim.vsix"))
            {
                var parts = package.GetParts().ToArray();
                Assert.Equal(21, parts.Length);
            }
        }

        [Fact]
        public void ShouldCreateSignatureBuilder()
        {
            using (var package = OpcPackage.Open(@"sample\VsVim.vsix"))
            {
                var builder = package.CreateSignatureBuilder();
                foreach(var part in package.GetParts())
                {
                    builder.EnqueuePart(part);
                    Assert.True(builder.DequeuePart(part));
                }
            }
        }

        [Theory]
        [InlineData("VsVim.dll")]
        [InlineData("/VsVim.dll")]
        [InlineData("package:///VsVim.dll")]
        public void ShouldOpenSinglePartByRelativeUri(string uri)
        {
            var partUri = new Uri(uri, UriKind.RelativeOrAbsolute);
            using (var package = OpcPackage.Open(@"sample\VsVim.vsix"))
            {
                Assert.NotNull(package.GetPart(partUri));
            }
        }

        [Fact]
        public void ShouldSignFile()
        {
            using (var package = ShadowCopyPackage(@"sample\VsVim.vsix", out _, OpcPackageFileMode.ReadWrite))
            {
                var builder = package.CreateSignatureBuilder();
                foreach(var part in package.GetParts())
                {
                    builder.EnqueuePart(part);
                }
                builder.Sign(HashAlgorithmName.SHA256, new X509Certificate2("sample\\cert.pfx", "test"));
                package.Flush();
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
