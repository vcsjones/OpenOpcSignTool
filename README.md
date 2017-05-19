OpenVsixSignTool
================


OpenVsixSignTool ("OVST") is an open-source implemention of [VsixSignTool][1] to digitally sign VSIX packages.

It offers a number of benefits, such as easily using certificates from hardware tokens, HSMs, etc by allowing
any certificate from the Certificate Store to be used instead of a PFX.

This app is currently in beta, however it works. See "Known Issues" below.

## Using

Using OVST is fairly simple. An example:

```shell
OpenVsixSignTool sign --sha1 7213125958254779abbaa5033a12fecdf2c7cdc8 --timestamp http://timestamp.digicert.com -ta sha256 -fd sha256 myvsix.vsix
```

This signs the VSIX using a certificate in the certificate store using the SHA1 thumbprint, and uses a SHA256
file digest and SHA256 timestamp digest algorithm.

For more information about usage, use `OpenVsixSignTool sign --help` for more information.

## Azure Key Vault Unit Tests

The Azure Key Vault unit tests depend on a file called `azure-creds.json` in the
`tests\OpenVsixSignTool.Core.Tests\private` directory. The file should look something like this:

```json
{
  "ClientId": "abcd1234-5678-90ef-bebe-ab1234567890",
  "ClientSecret": "your-awesome-appid-secret",
  "AzureKeyVaultUrl": "https://vault-name.vault.azure.net",
  "AzureKeyVaultCertificateName": "Certificate-Name"
}
```

This file will automatically be ignored by Git so it isn't accidentally commited, but still please take care to review any
commits to ensure this didn't accidentally get added somehow.

## Known Issues

See the list of [bugs][2] in GitHub for known bugs.

[1]: https://www.nuget.org/packages/Microsoft.VSSDK.Vsixsigntool/
[2]: https://github.com/vcsjones/OpenVsixSignTool/issues?q=is%3Aissue+is%3Aopen+label%3Abug