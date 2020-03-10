OpenOpcSignTool
================

[![Build Status](https://vcsjones.visualstudio.com/OpenOpcSignTool/_apis/build/status/OpenOpcSignTool-CI)](https://vcsjones.visualstudio.com/OpenOpcSignTool/_build/latest?definitionId=2)

OpenOpcSignTool ("OOST") is an open-source implemention of [VsixSignTool][1] to digitally sign VSIX packages on any platform,
with additional "OPC" package signing options to come.

It offers a number of benefits, such as easily using certificates from hardware tokens, HSMs, Azure Key Vault, etc by allowing
any certificate from the Certificate Store to be used instead of a PFX.

## Installing

Using .NET Core 2.1 or later:

```shell
dotnet tool install -g OpenVsixSignTool
```

Alternatively, it can be built by itself

## Using

Using OOST is fairly simple. An example:

```shell
OpenVsixSignTool sign --sha1 7213125958254779abbaa5033a12fecdf2c7cdc8 --timestamp http://timestamp.digicert.com -ta sha256 -fd sha256 myvsix.vsix
```

This signs the VSIX using a certificate in the certificate store using the SHA1 thumbprint, and uses a SHA256
file digest and SHA256 timestamp digest algorithm.

```shell
OpenVsixSignTool sign --subjectname "My Certificate" --timestamp http://timestamp.digicert.com -ta sha256 -fd sha256 myvsix.vsix
```

This signs the VSIX using a certificate in the certificate store using the subject name.

For more information about usage, use `OpenVsixSignTool sign --help` for more information.

## Core Library

This repository is broken out into two projects.

### Core Signing Library

The core library performs the signing functionality and offers a .NET API for programmatically signing a VSIX file. A sample for signing and timestamping
with an `X509Certificate2` would look like this:

```csharp
X509Certificate2 certificate = default; // Use a real instance of an X509Certificate2 with a private key
var configuration = new SignConfigurationSet(
	HashAlgorithmName.SHA256,
	HashAlgorithmName.SHA256,
	certificate.GetRSAPrivateKey(),
	certificate);
using (var package = OpcPackage.Open(@"C:\path\to\file.vsix", OpcPackageFileMode.ReadWrite))
{
	var builder = package.CreateSignatureBuilder();
	builder.EnqueueNamedPreset<VSIXSignatureBuilderPreset>();
	var signature = builder.Sign(configuration);
	// Apply a timestamp
	var timestampBuilder = signature.CreateTimestampBuilder();
	var result = await timestampBuilder.SignAsync(new Uri("http://timestamp.digicert.com"), HashAlgorithmName.SHA256);
	if (result != TimestampResult.Success)
	{
		throw new InvalidOperationException("Failed to timestamp the signature.");
	}
}
certificate.Dispose();
```


You can also use Azure Key Vault to sign a VSIX when using the [`RSAKeyVault`][3] NuGet package. Since the `SignConfigurationSet`
accepts a private key that is distinct from the certificate, the private key can be any implementation of `RSA` or `ECDsa`
that are properly implemented.

### CLI Tool

The command line tool uses the core library to offer CLI usage of the core library. It uses [`RSAKeyVault`][3] to achieve signing
with `AzureKeyVault`.

## Known Issues

See the list of [bugs][2] in GitHub for known bugs.

[1]: https://www.nuget.org/packages/Microsoft.VSSDK.Vsixsigntool/
[2]: https://github.com/vcsjones/OpenVsixSignTool/issues?q=is%3Aissue+is%3Aopen+label%3Abug
[3]: https://www.nuget.org/packages/RSAKeyVaultProvider/
