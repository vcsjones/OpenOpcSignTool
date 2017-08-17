using System;
using Xunit;

namespace OpenVsixSignTool.Core.Tests
{
    public class UriHelpersTests
    {
        [Theory]
        [InlineData("package:///file.bin", "file.bin")]
        [InlineData("package:/file.bin", "file.bin")]
        [InlineData("package:///sub/file.bin", "sub/file.bin")]
        [InlineData("package:/sub/file.bin", "sub/file.bin")]
        [InlineData("package:///sub/file.bin?query=string", "sub/file.bin")]
        [InlineData("package:/sub/file.bin?query=string", "sub/file.bin")]
        public void ShouldHandlePackagePathForRelativeUris(string uri, string expected)
        {
            var part = new Uri(uri, UriKind.Absolute);
            var packagePath = part.ToPackagePath();
            Assert.Equal(expected, packagePath);
        }

        [Theory]
        [InlineData("package:///file.bin", "/file.bin")]
        [InlineData("package:/file.bin", "/file.bin")]
        [InlineData("package:///sub/file.bin", "/sub/file.bin")]
        [InlineData("package:/sub/file.bin", "/sub/file.bin")]
        [InlineData("package:///sub/file.bin?query=string", "/sub/file.bin?query=string")]
        [InlineData("package:/sub/file.bin?query=string", "/sub/file.bin?query=string")]
        public void ShouldHandleReferencePathForRelativeUris(string uri, string expected)
        {
            var part = new Uri(uri, UriKind.Absolute);
            var packagePath = part.ToQualifiedPath();
            Assert.Equal(expected, packagePath);
        }

        [Theory]
        [InlineData("package:///file.bin", "package:///", true)]
        [InlineData("package:///egg/file.bin", "package:///egg/", true)]
        [InlineData("package:///EGG/file.bin", "package:///egg/", true)]
        [InlineData("package:///bird/file.bin", "package:///egg/", false)]
        [InlineData("package:///egg/bird/file.bin", "package:///egg/bird/", true)]
        [InlineData("package:///egg/bird/file.bin", "package:///egg/bird/file.bin", true)]
        [InlineData("package:///egg/bird/file.bin", "package:///egg/bird/file.bin/nest", false)]
        [InlineData("[trash]/foo.bin", "[trash]", true)]
        public void ShouldHandleContainsPaths(string child, string parent, bool expected)
        {
            var childUri = new Uri(child, UriKind.RelativeOrAbsolute);
            var parentUri = new Uri(parent, UriKind.RelativeOrAbsolute);
            Assert.Equal(expected, childUri.EqualOrContainedBy(parentUri));

        }
    }
}
