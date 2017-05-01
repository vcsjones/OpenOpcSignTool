using Microsoft.Extensions.CommandLineUtils;
using System;

namespace OpenVsixSignTool
{
    class Program
    {
        static int Main(string[] args)
        {
            var application = new CommandLineApplication(throwOnUnexpectedArg: false);
            var signCommand = application.Command("sign", throwOnUnexpectedArg: false, configuration: signConfiguration =>
            {
                signConfiguration.Description = "Signs a VSIX package.";
                signConfiguration.HelpOption("-? | -h | --help");
                var sha1 = signConfiguration.Option("-s | --sha1", "A hex-encoded SHA-1 thumbprint of the certificate used to sign the executable.", CommandOptionType.SingleValue);
                var pfxPath = signConfiguration.Option("-c | --certificate", "A path to a PFX file to perform the signature.", CommandOptionType.SingleValue);
                var password = signConfiguration.Option("-p | --password", "The password for the PFX file.", CommandOptionType.SingleValue);
                var timestamp = signConfiguration.Option("-t | --timestamp", "A URL of the timestamping server to timestamp the signature.", CommandOptionType.SingleValue);
                var timestampAlgorithm = signConfiguration.Option("-ta | --timestamp-algorithm", "The digest algorithm of the timestamp.", CommandOptionType.SingleValue);
                var fileDigest = signConfiguration.Option("-fd | --file-digest", "A URL of the timestamping server to timestamp the signature.", CommandOptionType.SingleValue);
                var force = signConfiguration.Option("-f | --force", "Force the signature by overwriting any existing signatures.", CommandOptionType.NoValue);
                var file = signConfiguration.Argument("file", "A path to the PFX used to sign the VSIX file.");
                signConfiguration.OnExecute(() =>
                {
                    return new SignCommand(signConfiguration).Sign(sha1, pfxPath, password, timestamp, timestampAlgorithm, fileDigest, force, file);
                });
            });
            application.HelpOption("-? | -h | --help");
            application.VersionOption("-v | --version", typeof(Program).Assembly.GetName().Version.ToString(3));
            if (args.Length == 0)
            {
                application.ShowHelp();
            }
            return application.Execute(args);
        }
    }
}