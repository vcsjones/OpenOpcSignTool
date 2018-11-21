using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.CommandLineUtils;
using OpenVsixSignTool.Core;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace OpenVsixSignTool
{
    class SignCommand
    {
        internal static class EXIT_CODES
        {
            public const int SUCCESS = 0;
            public const int INVALID_OPTIONS = 1;
            public const int FAILED = 2;
        }

        private readonly CommandLineApplication _signCommandApplication;

        public SignCommand(CommandLineApplication signCommandApplication)
        {
            _signCommandApplication = signCommandApplication;
        }

        internal Task<int> SignAsync
        (
            CommandOption sha1,
            CommandOption pfxPath,
            CommandOption password,
            CommandOption timestampUrl,
            CommandOption timestampAlgorithm,
            CommandOption fileDigest,
            CommandOption force,
            CommandArgument vsixPath)
        {
            if (!(sha1.HasValue() ^ pfxPath.HasValue()))
            {
                _signCommandApplication.Out.WriteLine("Either --sha1 or --certificate must be specified, but not both.");
                _signCommandApplication.ShowHelp();
                return Task.FromResult(EXIT_CODES.INVALID_OPTIONS);
            }
            X509Certificate2 certificate;
            if (sha1.HasValue())
            {
                certificate = GetCertificateFromCertificateStore(sha1.Value());
                if (certificate == null)
                {
                    _signCommandApplication.Out.WriteLine("Unable to locate certificate by thumbprint.");
                    return Task.FromResult(EXIT_CODES.FAILED);
                }
            }
            else
            {
                var pfxFilePath = pfxPath.Value();
                if (!File.Exists(pfxFilePath))
                {
                    _signCommandApplication.Out.WriteAsync("Specified PFX file does not exist.");
                    return Task.FromResult(EXIT_CODES.INVALID_OPTIONS);
                }
                if (!password.HasValue())
                {
                    certificate = new X509Certificate2(pfxFilePath);
                }
                else
                {
                    certificate = new X509Certificate2(pfxFilePath, password.Value());
                }
            }
            Uri timestampServer = null;
            if (timestampUrl.HasValue())
            {
                if (!Uri.TryCreate(timestampUrl.Value(), UriKind.Absolute, out timestampServer))
                {
                    _signCommandApplication.Out.WriteLine("Specified timestamp URL is invalid.");
                    return Task.FromResult(EXIT_CODES.FAILED);
                }
                if (timestampServer.Scheme != Uri.UriSchemeHttp && timestampServer.Scheme != Uri.UriSchemeHttps)
                {
                    _signCommandApplication.Out.WriteLine("Specified timestamp URL is invalid.");
                    return Task.FromResult(EXIT_CODES.FAILED);
                }
            }
            var vsixPathValue = vsixPath.Value;
            if (!File.Exists(vsixPathValue))
            {
                _signCommandApplication.Out.WriteLine("Specified file does not exist.");
                return Task.FromResult(EXIT_CODES.FAILED);
            }
            HashAlgorithmName fileDigestAlgorithm, timestampDigestAlgorithm;
            var fileDigestResult = AlgorithmFromInput(fileDigest.HasValue() ? fileDigest.Value() : null);
            if (fileDigestResult == null)
            {
                _signCommandApplication.Out.WriteLine("Specified file digest algorithm is not supported.");
                return Task.FromResult(EXIT_CODES.INVALID_OPTIONS);
            }
            else
            {
                fileDigestAlgorithm = fileDigestResult.Value;
            }
            var timestampDigestResult = AlgorithmFromInput(timestampAlgorithm.HasValue() ? timestampAlgorithm.Value() : null);
            if (timestampDigestResult == null)
            {
                _signCommandApplication.Out.WriteLine("Specified timestamp digest algorithm is not supported.");
                return Task.FromResult(EXIT_CODES.INVALID_OPTIONS);
            }
            else
            {
                timestampDigestAlgorithm = timestampDigestResult.Value;
            }
            return PerformSignOnVsixAsync(vsixPathValue, force.HasValue(), timestampServer, fileDigestAlgorithm, timestampDigestAlgorithm,
                certificate, GetSigningKeyFromCertificate(certificate));
        }

        internal async Task<int> SignAzure(CommandOption azureKeyVaultUrl, CommandOption azureKeyVaultClientId,
            CommandOption azureKeyVaultClientSecret, CommandOption azureKeyVaultCertificateName, CommandOption azureKeyVaultAccessToken, CommandOption force,
            CommandOption fileDigest, CommandOption timestampUrl, CommandOption timestampAlgorithm, CommandArgument vsixPath)
        {
            if (!azureKeyVaultUrl.HasValue())
            {
                _signCommandApplication.Out.WriteLine("The Azure Key Vault URL must be specified for Azure signing.");
                return EXIT_CODES.INVALID_OPTIONS;
            }


            // we only need the client id/secret if we don't have an access token
            if (!azureKeyVaultAccessToken.HasValue())
            {
                if (!azureKeyVaultClientId.HasValue())
                {
                    _signCommandApplication.Out.WriteLine("The Azure Key Vault Client ID or Access Token must be specified for Azure signing.");
                    return EXIT_CODES.INVALID_OPTIONS;
                }

                if (!azureKeyVaultClientSecret.HasValue())
                {
                    _signCommandApplication.Out.WriteLine("The Azure Key Vault Client Secret or Access Token must be specified for Azure signing.");
                    return EXIT_CODES.INVALID_OPTIONS;
                }
            }

            if (!azureKeyVaultCertificateName.HasValue())
            {
                _signCommandApplication.Out.WriteLine("The Azure Key Vault Client Certificate Name must be specified for Azure signing.");
                return EXIT_CODES.INVALID_OPTIONS;
            }
            Uri timestampServer = null;
            if (timestampUrl.HasValue())
            {
                if (!Uri.TryCreate(timestampUrl.Value(), UriKind.Absolute, out timestampServer))
                {
                    _signCommandApplication.Out.WriteLine("Specified timestamp URL is invalid.");
                    return EXIT_CODES.FAILED;
                }
                if (timestampServer.Scheme != Uri.UriSchemeHttp && timestampServer.Scheme != Uri.UriSchemeHttps)
                {
                    _signCommandApplication.Out.WriteLine("Specified timestamp URL is invalid.");
                    return EXIT_CODES.FAILED;
                }
            }
            var vsixPathValue = vsixPath.Value;
            if (!File.Exists(vsixPathValue))
            {
                _signCommandApplication.Out.WriteLine("Specified file does not exist.");
                return EXIT_CODES.FAILED;
            }
            HashAlgorithmName fileDigestAlgorithm, timestampDigestAlgorithm;
            var fileDigestResult = AlgorithmFromInput(fileDigest.HasValue() ? fileDigest.Value() : null);
            if (fileDigestResult == null)
            {
                _signCommandApplication.Out.WriteLine("Specified file digest algorithm is not supported.");
                return EXIT_CODES.INVALID_OPTIONS;
            }
            else
            {
                fileDigestAlgorithm = fileDigestResult.Value;
            }
            var timestampDigestResult = AlgorithmFromInput(timestampAlgorithm.HasValue() ? timestampAlgorithm.Value() : null);
            if (timestampDigestResult == null)
            {
                _signCommandApplication.Out.WriteLine("Specified timestamp digest algorithm is not supported.");
                return EXIT_CODES.INVALID_OPTIONS;
            }
            else
            {
                timestampDigestAlgorithm = timestampDigestResult.Value;
            }
            var configuration = new AzureKeyVaultSignConfigurationSet
            {
                AzureKeyVaultUrl = azureKeyVaultUrl.Value(),
                AzureKeyVaultCertificateName = azureKeyVaultCertificateName.Value(),
                AzureClientId = azureKeyVaultClientId.Value(),
                AzureAccessToken = azureKeyVaultAccessToken.Value(),
                AzureClientSecret = azureKeyVaultClientSecret.Value(),
            };

            var configurationDiscoverer = new KeyVaultConfigurationDiscoverer();
            var materializedResult = await configurationDiscoverer.Materialize(configuration);
            AzureKeyVaultMaterializedConfiguration materialized;
            switch (materializedResult)
            {
                case ErrorOr<AzureKeyVaultMaterializedConfiguration>.Ok ok:
                    materialized = ok.Value;
                    break;
                default:
                    _signCommandApplication.Out.WriteLine("Failed to get configuration from Azure Key Vault.");
                    return EXIT_CODES.FAILED;
            }
            var context = new KeyVaultContext(materialized.Client, materialized.KeyId, materialized.PublicCertificate);
            using (var keyVault = new RSAKeyVault(context))
            {
                return await PerformSignOnVsixAsync(
                    vsixPathValue,
                    force.HasValue(),
                    timestampServer,
                    fileDigestAlgorithm,
                    timestampDigestAlgorithm,
                    materialized.PublicCertificate,
                    keyVault
                );
            }
        }

        private async Task<int> PerformSignOnVsixAsync(string vsixPath, bool force,
            Uri timestampUri, HashAlgorithmName fileDigestAlgorithm, HashAlgorithmName timestampDigestAlgorithm,
            X509Certificate2 certificate, AsymmetricAlgorithm signingKey
            )
        {
            using (var package = OpcPackage.Open(vsixPath, OpcPackageFileMode.ReadWrite))
            {
                if (package.GetSignatures().Any() && !force)
                {
                    _signCommandApplication.Out.WriteLine("The VSIX is already signed.");
                    return EXIT_CODES.FAILED;
                }
                var signBuilder = package.CreateSignatureBuilder();
                signBuilder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
                var signingConfiguration = new SignConfigurationSet
                {
                    FileDigestAlgorithm = fileDigestAlgorithm,
                    PkcsDigestAlgorithm = fileDigestAlgorithm,
                    SigningCertificate = certificate,
                    SigningKey = signingKey
                };

                var signature = signBuilder.Sign(signingConfiguration);
                if (timestampUri != null)
                {
                    var timestampBuilder = signature.CreateTimestampBuilder();
                    var result = await timestampBuilder.SignAsync(timestampUri, timestampDigestAlgorithm);
                    if (result == TimestampResult.Failed)
                    {
                        return EXIT_CODES.FAILED;
                    }
                }
                _signCommandApplication.Out.WriteLine("The signing operation is complete.");
                return EXIT_CODES.SUCCESS;
            }
        }

        private static HashAlgorithmName? AlgorithmFromInput(string value)
        {
            switch (value?.ToLower())
            {
                case "sha1":
                    return HashAlgorithmName.SHA1;
                case "sha384":
                    return HashAlgorithmName.SHA384;
                case "sha512":
                    return HashAlgorithmName.SHA512;
                case null:
                case "sha256":
                    return HashAlgorithmName.SHA256;
                default:
                    return null;

            }
        }

        private static AsymmetricAlgorithm GetSigningKeyFromCertificate(X509Certificate2 certificate)
        {
            const string RSA = "1.2.840.113549.1.1.1";
            const string Ecc = "1.2.840.10045.2.1";
            var keyAlgorithm = certificate.GetKeyAlgorithm();
            switch (keyAlgorithm)
            {
                case RSA:
                    return certificate.GetRSAPrivateKey();
                case Ecc:
                    return certificate.GetECDsaPrivateKey();
                default:
                    throw new InvalidOperationException("Unknown certificate signing algorithm.");
            }
        }

        private static X509Certificate2 GetCertificateFromCertificateStore(string sha1)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, sha1, false);
                if (certificates.Count > 0)
                {
                    return certificates[0];
                }
            }

            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, sha1, false);
                if (certificates.Count == 0)
                {
                    return null;
                }
                return certificates[0];
            }

        }
    }
}
