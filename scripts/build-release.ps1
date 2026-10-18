param([string]$PublishDirectory='artifacts/publish/win-x64')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
function Run-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}
Run-Dotnet restore FrameForge.sln --locked-mode
Run-Dotnet build FrameForge.sln -c Release --no-restore '-m:2'
Run-Dotnet test tests/FrameForge.Tests -c Release --no-build --logger 'trx;LogFileName=release-tests.trx' --results-directory artifacts/test-results
Run-Dotnet publish src/FrameForge.App -c Release -r win-x64 --self-contained true --no-restore -o $PublishDirectory
& (Join-Path $PSScriptRoot 'validate-windows.ps1') -PublishDirectory $PublishDirectory
& python -m unittest discover -s scripts -p 'test_*.py'
if ($LASTEXITCODE -ne 0) { throw 'Packaging tests failed' }
& python scripts/package.py --publish-folder $PublishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Packaging failed' }
