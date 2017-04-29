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
    public class OpcPackageTests : IDisposable
    {
        private const string SamplePackage = @"sample\OpenVsixSignToolTest.vsix";
        private const string SamplePackageSigned = @"sample\OpenVsixSignToolTest-Signed.vsix";
        private readonly List<string> _shadowFiles = new List<string>();

        [Fact]
        public void ShouldOpenAndDisposeAPackageAndDisposeIsIdempotent()
        {
            var package = OpcPackage.Open(SamplePackage);
            package.Dispose();
            package.Dispose();
        }

        [Fact]
        public void ShouldReadContentTypes()
        {
            using (var package = OpcPackage.Open(SamplePackage))
            {
                Assert.Equal(3, package.ContentTypes.Count);
                var first = package.ContentTypes[0];
                Assert.Equal("vsixmanifest", first.Extension);
                Assert.Equal("text/xml", first.ContentType);
                Assert.Equal(OpcContentTypeMode.Default, first.Mode);
            }
        }

        [Fact]
        public void ShouldNotAllowUpdatingContentTypesInReadOnly()
        {
            using (var package = OpcPackage.Open(SamplePackage))
            {
                var newItem = new OpcContentType("test", "test", OpcContentTypeMode.Default);
                var contentTypes = package.ContentTypes;
                Assert.Throws<InvalidOperationException>(() => contentTypes.Add(newItem));
            }
        }

        [Fact]
        public void ShouldAllowUpdatingContentType()
        {
            int initialCount;
            string shadowPath;
            using (var package = ShadowCopyPackage(SamplePackage, out shadowPath, OpcPackageFileMode.ReadWrite))
            {
                initialCount = package.ContentTypes.Count;
                var newItem = new OpcContentType("test", "application/test", OpcContentTypeMode.Default);
                package.ContentTypes.Add(newItem);
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
            using (var package = ShadowCopyPackage(SamplePackage, out shadowPath, OpcPackageFileMode.ReadWrite))
            {
                initialCount = package.Relationships.Count;
                var newItem = new OpcRelationship(new Uri("/test", UriKind.RelativeOrAbsolute), new Uri("/test", UriKind.RelativeOrAbsolute));
                package.Relationships.Add(newItem);
                Assert.True(newItem.Id != null && newItem.Id.Length == 9);
            }
            using (var reopenedPackage = OpcPackage.Open(shadowPath))
            {
                Assert.Equal(initialCount + 1, reopenedPackage.Relationships.Count);
            }
        }

        [Fact]
        public void ShouldEnumerateAllParts()
        {
            using (var package = OpcPackage.Open(SamplePackage))
            {
                var parts = package.GetParts().ToArray();
                Assert.Equal(1, parts.Length);
            }
        }

        [Fact]
        public void ShouldCreateSignatureBuilder()
        {
            using (var package = OpcPackage.Open(SamplePackage))
            {
                var builder = package.CreateSignatureBuilder();
                foreach (var part in package.GetParts())
                {
                    builder.EnqueuePart(part);
                    Assert.True(builder.DequeuePart(part));
                }
            }
        }

        [Theory]
        [InlineData("extension.vsixmanifest")]
        [InlineData("/extension.vsixmanifest")]
        [InlineData("package:///extension.vsixmanifest")]
        public void ShouldOpenSinglePartByRelativeUri(string uri)
        {
            var partUri = new Uri(uri, UriKind.RelativeOrAbsolute);
            using (var package = OpcPackage.Open(SamplePackage))
            {
                Assert.NotNull(package.GetPart(partUri));
            }
        }

        [Fact]
        public void ShouldReturnEmptyEnumerableForNoSignatureOriginRelationship()
        {
            using (var package = OpcPackage.Open(SamplePackage, OpcPackageFileMode.Read))
            {
                Assert.Empty(package.GetSignatures());
            }
        }

        [Fact]
        public void ShouldReturnSignatureForSignedPackage()
        {
            using (var package = OpcPackage.Open(SamplePackageSigned, OpcPackageFileMode.Read))
            {
                Assert.NotEmpty(package.GetSignatures());
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
