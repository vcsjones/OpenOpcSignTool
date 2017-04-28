OpenVisxSignTool
================


OpenVsixSignTool ("OVST") is an open-source implement of [VsixSignTool][1] to digitally sign VSIX packages.

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

## Known Issues

See the list of [bugs][2] in GitHub for known bugs.

[1]: https://www.nuget.org/packages/Microsoft.VSSDK.Vsixsigntool/
[2]: https://github.com/vcsjones/OpenVsixSignTool/issues?q=is%3Aissue+is%3Aopen+label%3Abug