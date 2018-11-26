using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace OpenVsixSignTool.Tests
{
    internal sealed class ConsoleIntercepter : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly TextWriter _new;

        public ConsoleIntercepter()
        {
            _writer = Console.Out;
            _new = new StringWriter();
            Console.SetOut(_new);
        }

        public string Content => _new.ToString();

        public void Dispose()
        {
            Console.SetOut(_writer);
            _new.Dispose();
        }
    }

    public class SignIntegrationTests : IDisposable
    {
        private readonly List<string> _shadowFiles = new List<string>();
        private static string SamplePath(string str) => Path.Combine("sample", str);
        private static string CertPath(string str) => Path.Combine("certs", str);

        [Theory]
        [MemberData(nameof(HandleInvalidCommandLineOptionsTheories))]
        public void ShouldHandleInvalidCommandLineOptions(string[] args, int expectedExitCode, string expectedMessage)
        {
            using (var consoleWriter = new ConsoleIntercepter())
            {
                var shadow = ShadowCopyPackage(SamplePath("OpenVsixSignToolTest.vsix"));
                Assert.Equal(expectedExitCode, Program.Main(args.Concat(new[] {shadow}).ToArray()));
                Assert.Contains(expectedMessage, consoleWriter.Content);
            }
        }

        [Theory]
        [MemberData(nameof(HandleValidCommandLineOptionsTheories))]
        public void ShouldHandleValidCommandLineOptions(string[] args, string expectedMessage)
        {
            using (var consoleWriter = new ConsoleIntercepter())
            {
                var shadow = ShadowCopyPackage(SamplePath("OpenVsixSignToolTest.vsix"));
                Assert.Equal(0, Program.Main(args.Concat(new[] { shadow }).ToArray()));
                Assert.Contains(expectedMessage, consoleWriter.Content);
            }
        }

        [Theory]
        [MemberData(nameof(HandleRepeatedCommandLineOptionsTheories))]
        public void ShouldHandleRepeatedCommandLineOptions((string[] args, string expectedMessage, int expectedExitCode)[] argsSets)
        {
            var shadow = ShadowCopyPackage(SamplePath("OpenVsixSignToolTest.vsix"));
            foreach (var (args, expectedMessage, expectedExitCode) in argsSets)
            {
                using (var consoleWriter = new ConsoleIntercepter())
                {
                    Assert.Equal(expectedExitCode, Program.Main(args.Concat(new[] {shadow}).ToArray()));
                    Assert.Contains(expectedMessage, consoleWriter.Content);
                }
            }
        }

        public static IEnumerable<object[]> HandleValidCommandLineOptionsTheories
        {
            get
            {
                //Normal signature with PFX
                yield return new object[]
                {
                    new[] {"sign", "-c", CertPath("rsa-2048-sha256.pfx"), "-p", "test"}, "The signing operation is complete."
                };
            }
        }

        public static IEnumerable<object[]> HandleRepeatedCommandLineOptionsTheories
        {
            get
            {
                //Sign it once, then try signing it again without the force option.
                yield return new object[]
                {
                    new[]
                    {
                        (new [] {"sign", "-c", CertPath("rsa-2048-sha256.pfx"), "-p", "test"}, "The signing operation is complete.", 0),
                        (new [] {"sign", "-c", CertPath("rsa-2048-sha256.pfx"), "-p", "test"}, "The VSIX is already signed.", 2)
                    }
                };
                //Sign it once, then try signing it again with the force option.
                yield return new object[]
                {
                    new[]
                    {
                        (new [] {"sign", "-c", CertPath("rsa-2048-sha256.pfx"), "-p", "test"}, "The signing operation is complete.", 0),
                        (new [] {"sign", "-c", CertPath("rsa-2048-sha256.pfx"), "-p", "test", "-f"}, "The signing operation is complete.", 0)
                    }
                };
                //Sign it then unsign it
                yield return new object[]
                {
                    new[]
                    {
                        (new [] {"sign", "-c", CertPath("rsa-2048-sha256.pfx"), "-p", "test"}, "The signing operation is complete.", 0),
                        (new [] {"unsign" }, "The unsigning operation is complete.", 0)
                    }
                };
            }
        }

        public static IEnumerable<object[]> HandleInvalidCommandLineOptionsTheories
        {
            get
            {
                //Path to PFX does not exist.
                yield return new object[]
                {
                    new [] { "sign", "-c", CertPath("idontexist.pfx"), "-p", "test" }, 1, "Specified PFX file does not exist."
                };
                //Only password specified.
                yield return new object[]
                {
                    new[] { "sign", "-p", "blah" }, 1, "Either --sha1 or --certificate must be specified, but not both."
                };
                //Both SHA1 thumbprint and PFX were specified.
                yield return new object[]
                {
                    new[] { "sign", "-c", "blah", "-s", "blah" }, 1, "Either --sha1 or --certificate must be specified, but not both."
                };
                //Invalid file digest algorithm
                yield return new object[]
                {
                    new [] { "sign", "-c", CertPath("rsa-2048-sha256.pfx"), "-p", "test", "-fd", "md2" }, 1, "Specified file digest algorithm is not supported."
                };
            }
        }

        private string ShadowCopyPackage(string packagePath)
        {
            var temp = Path.GetTempFileName();
            _shadowFiles.Add(temp);
            File.Copy(packagePath, temp, true);
            return temp;
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
