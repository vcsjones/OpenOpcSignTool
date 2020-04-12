using Microsoft.Extensions.CommandLineUtils;

namespace OpenVsixSignTool
{
    static class Program
    {
        internal static int Main(string[] args)
        {
            var application = new CommandLineApplication(throwOnUnexpectedArg: false);
            var signCommand = application.Command("sign", throwOnUnexpectedArg: false, configuration: signConfiguration =>
                {
                    signConfiguration.Description = "Signs a VSIX package.";
                    signConfiguration.HelpOption("-? | -h | --help");
                    var sha1 = signConfiguration.Option("-s | --sha1", "A hex-encoded SHA-1 thumbprint of the certificate used to sign the opc file.", CommandOptionType.SingleValue);
                    var pfxPath = signConfiguration.Option("-c | --certificate", "A path to a PFX file to perform the signature.", CommandOptionType.SingleValue);
                    var password = signConfiguration.Option("-p | --password", "The password for the PFX file.", CommandOptionType.SingleValue);
                    var timestamp = signConfiguration.Option("-t | --timestamp", "A URL of the timestamping server to timestamp the signature.", CommandOptionType.SingleValue);
                    var timestampAlgorithm = signConfiguration.Option("-ta | --timestamp-algorithm", "The digest algorithm of the timestamp.", CommandOptionType.SingleValue);
                    var fileDigest = signConfiguration.Option("-fd | --file-digest", "The digest algorithm to hash the opc file with.", CommandOptionType.SingleValue);
                    var force = signConfiguration.Option("-f | --force", "Force the signature by overwriting any existing signatures.", CommandOptionType.NoValue);
                    var file = signConfiguration.Argument("file", "A to the VSIX file.");

                    var azureKeyVaultUrl = signConfiguration.Option("-kvu | --azure-key-vault-url", "The URL to an Azure Key Vault.", CommandOptionType.SingleValue);
                    var azureKeyVaultClientId = signConfiguration.Option("-kvi | --azure-key-vault-client-id", "The Client ID to authenticate to the Azure Key Vault.", CommandOptionType.SingleValue);
                    var azureKeyVaultClientSecret = signConfiguration.Option("-kvs | --azure-key-vault-client-secret", "The Client Secret to authenticate to the Azure Key Vault.", CommandOptionType.SingleValue);
                    var azureKeyVaultCertificateName = signConfiguration.Option("-kvc | --azure-key-vault-certificate", "The name of the certificate in Azure Key Vault.", CommandOptionType.SingleValue);
                    var azureKeyVaultAccessToken = signConfiguration.Option("-kva | --azure-key-vault-accesstoken", "The Access Token to authenticate to the Azure Key Vault.", CommandOptionType.SingleValue);

                    signConfiguration.OnExecute(() =>
                    {
                        var sign = new SignCommand(signConfiguration);
                        if (sha1.HasValue() || pfxPath.HasValue() || password.HasValue() || pfxPath.HasValue())
                        {
                            return sign.SignAsync(sha1, pfxPath, password, timestamp, timestampAlgorithm, fileDigest, force, file);
                        }
                        else
                        {
                            return sign.SignAzure(azureKeyVaultUrl, azureKeyVaultClientId, azureKeyVaultClientSecret,
                                azureKeyVaultCertificateName, azureKeyVaultAccessToken, force, fileDigest, timestamp, timestampAlgorithm, file);
                        }
                    });
                }
            );
            var unsignCommand = application.Command("unsign", throwOnUnexpectedArg: false, configuration: unsignConfiguration =>
                {
                    unsignConfiguration.Description = "Removes all signatures from a VSIX package.";
                    unsignConfiguration.HelpOption("-? | -h | --help");
                    var file = unsignConfiguration.Argument("file", "A path to the VSIX file.");
                    unsignConfiguration.OnExecute(() =>
                    {
                        return new UnsignCommand(unsignConfiguration).Unsign(file);
                    });
                }
            );
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
