dotnet restore $PSScriptRoot
If ($lastexitcode -ne 0) {
	Write-Host "Restore failed."
	exit 1
}
dotnet test --framework net462 $PSScriptRoot\tests\OpenVsixSignTool.Core.Tests\OpenVsixSignTool.Core.Tests.csproj
$CoreTestsFailed = $lastexitcode

dotnet test --framework net462 $PSScriptRoot\tests\OpenVsixSignTool.Tests\OpenVsixSignTool.Tests.csproj
$IntegrationTestsFailed = $lastexitcode

If (($CoreTestsFailed -ne 0) -or ($IntegrationTestsFailed -ne 0)) {
	Write-Host "Tests failed."
	exit 1
}