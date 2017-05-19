using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using OpenVsixSignTool.Core;

namespace OpenVsixSignTool
{
    internal class UnsignCommand
    {
        internal static class EXIT_CODES
        {
            public const int SUCCESS = 0;
            public const int INVALID_OPTIONS = 1;
            public const int FAILED = 2;
        }

        private readonly CommandLineApplication _unsignConfiguration;

        public UnsignCommand(CommandLineApplication unsignConfiguration)
        {
            _unsignConfiguration = unsignConfiguration;
        }

        public int Unsign(CommandArgument vsixPath)
        {
            var vsixPathValue = vsixPath.Value;
            if (!File.Exists(vsixPathValue))
            {
                _unsignConfiguration.Out.WriteLine("Specified file does not exist.");
                return SignCommand.EXIT_CODES.INVALID_OPTIONS;
            }
            using (var package = OpcPackage.Open(vsixPathValue, OpcPackageFileMode.ReadWrite))
            {
                var unsigned = false;
                foreach (var signature in package.GetSignatures())
                {
                    unsigned = true;
                    signature.Remove();
                }
                if (!unsigned)
                {
                    _unsignConfiguration.Out.WriteLine("Specified VSIX is not signed.");
                    return EXIT_CODES.FAILED;
                }
                _unsignConfiguration.Out.WriteLine("The unsigning operation is complete.");
                return EXIT_CODES.SUCCESS;
            }
        }
    }
}
