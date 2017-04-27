using System;
using Xunit;

namespace OpenVsixSignTool.Tests
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
    }
}
