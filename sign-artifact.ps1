#Requires -Version 6.1

# PowerShell Core is required to run this script. This is to address an issue with how the
# .NET Framework creates ZIP files. It will use Windows directory separators, which will
# break a Nupkg.
param (
	[string]$thumbprint = 'c05862d4fe5fb8212c3c0e329108393a47c95e83',
	[Parameter(Mandatory=$true)][string]$nupkg
)

$package = Get-Item $nupkg
$asms = @('OpenVsixSignTool.Core.dll', 'OpenVsixSignTool.dll')
$path = Join-Path -Path $env:TEMP -ChildPath (New-Guid)
md -Force $path | Out-Null

# Expand-Archive requires the file have a .zip extension, so don't use it.
[System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null
[System.IO.Compression.ZipFile]::ExtractToDirectory($package.FullName, $path)

$filesToSign = Get-ChildItem -Path $path -Include $asms -Recurse
signtool sign /sha1 $thumbprint /fd sha256 /td sha256 /tr http://timestamp.digicert.com -d 'OpenVsixSignTool' -du 'https://github.com/vcsjones/OpenOpcSignTool' $filesToSign

$signedNupkg = Join-Path -Path $package.Directory -ChildPath "$($package.BaseName)-signed.nupkg"
if (Test-Path $signedNupkg) {
  Remove-Item $signedNupkg
}
[System.IO.Compression.ZipFile]::CreateFromDirectory($path, $signedNupkg);

# Reuse the temp directory for the nuget cli tool, we're done working with it.
$nugetTool = Join-Path -Path $path -ChildPath 'nuget.exe'
iwr "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetTool

& $nugetTool sign $signedNupkg -CertificateFingerprint $thumbprint -HashAlgorithm SHA256 -Timestamper http://timestamp.digicert.com -TimestampHashAlgorithm SHA256

Remove-Item -Recurse -Force $path
