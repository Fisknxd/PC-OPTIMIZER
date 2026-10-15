# Nondestructive acceptance checks for the published application, on Windows only.
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$exe = Join-Path (Get-Location) 'artifacts/publish/win-x64/FrameForge.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Publish FrameForge first using scripts/build-release.ps1.' }
$report = Join-Path (Get-Location) 'artifacts/ui-checks/report.txt'
if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
$interactions = Join-Path (Get-Location) 'artifacts/ui-checks/interactions.txt'
if (Test-Path -LiteralPath $interactions) { Remove-Item -LiteralPath $interactions }
$p = Start-Process -FilePath $exe -ArgumentList '--ui-test' -WorkingDirectory (Get-Location).Path -PassThru
if (-not $p.WaitForExit(60000)) { $p.Kill(); throw 'Windows menu/contrast checks timed out' }
if ($p.ExitCode -ne 0) {
    $failure = Join-Path (Get-Location) 'artifacts/ui-test-error.txt'
    if (Test-Path -LiteralPath $failure) {
        Get-Content -LiteralPath $failure
        $detail = (Get-Content -LiteralPath $failure -Raw) -replace '[\r\n]+', ' | '
        Write-Output ("::error::" + $detail.Substring(0, [Math]::Min(6000, $detail.Length)))
    }
    throw "Windows menu/contrast checks failed: $($p.ExitCode)"
}
if (-not (Test-Path -LiteralPath $report)) { throw 'No current-run UI validation report was produced.' }
if (-not (Test-Path -LiteralPath $interactions)) { throw 'No current-run switch interaction report was produced.' }
Get-Content -LiteralPath $interactions
Get-Content -LiteralPath $report
