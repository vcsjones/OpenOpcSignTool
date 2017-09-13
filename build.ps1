dotnet restore $PSScriptRoot
If ($lastexitcode -ne 0) {
	Write-Host "Restore failed."
	exit 1
}
pushd $PSScriptRoot\tests\OpenVsixSignTool.Core.Tests
dotnet xunit
$CoreTestsFailed = $lastexitcode
popd

pushd $PSScriptRoot\tests\OpenVsixSignTool.Tests
dotnet xunit
$IntegrationTestsFailed = $lastexitcode
popd

If (($CoreTestsFailed -ne 0) -or ($IntegrationTestsFailed -ne 0)) {
	Write-Host "Tests failed."
	exit 1
}