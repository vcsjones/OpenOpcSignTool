dotnet restore $PSScriptRoot
If ($lastexitcode -ne 0) {
	Write-Host "Restore failed."
	exit 1
}
dotnet test $PSScriptRoot\tests\OpenVsixSignTool.Core.Tests\OpenVsixSignTool.Core.Tests.csproj

If ($lastexitcode -ne 0) {
	Write-Host "Tests failed."
	exit 1
}