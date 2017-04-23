using System;
using System.Collections.Generic;
using System.IO;
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
                package.Save();
            }
            using (var reopenedPackage = OpcPackage.Open(shadowPath))
            {
                Assert.Equal(initialCount + 1, reopenedPackage.ContentTypes.Count);
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
