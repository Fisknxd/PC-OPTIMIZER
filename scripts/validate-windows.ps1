# Nondestructive acceptance checks for the published application, on Windows only.
param([string]$PublishDirectory='artifacts/publish/win-x64')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$exe = Join-Path (Get-Location) (Join-Path $PublishDirectory 'FrameForge.exe')
if (-not (Test-Path -LiteralPath $exe)) { throw 'Publish FrameForge first using scripts/build-release.ps1.' }
$qa = Join-Path (Get-Location) 'artifacts/ui-checks'
$requiredReports = @('report.txt','feature-toggles.txt','expanded-ui.txt','performance-ui.txt','quality-ui.txt','responsive-ui.txt','monitor-activity-ui.txt','recovery-ui.txt')
$report = Join-Path $qa 'report.txt'
foreach ($name in $requiredReports) { $old = Join-Path $qa $name; if (Test-Path -LiteralPath $old) { Remove-Item -LiteralPath $old } }
$failure = Join-Path (Get-Location) 'artifacts/ui-test-error.txt'
if (Test-Path -LiteralPath $failure) { Remove-Item -LiteralPath $failure }
$p = Start-Process -FilePath $exe -ArgumentList '--ui-test','--feature-test','--recovery-test' -WorkingDirectory (Get-Location).Path -WindowStyle Hidden -PassThru
if (-not $p.WaitForExit(60000)) { $p.Kill(); throw 'Windows menu/contrast checks timed out' }
if ($p.ExitCode -ne 0 -or (Test-Path -LiteralPath $failure)) {
    $failure = Join-Path (Get-Location) 'artifacts/ui-test-error.txt'
    if (Test-Path -LiteralPath $failure) { Get-Content -LiteralPath $failure }
    throw "Windows menu/contrast checks failed: $($p.ExitCode)"
}
foreach ($name in $requiredReports) { if (-not (Test-Path -LiteralPath (Join-Path $qa $name))) { throw ('No current-run UI validation report: '+$name) } }
Get-Content -LiteralPath $report

# Prove that a deliberately failed automated run returns failure independently of
# WPF's main-window shutdown behavior. Its expected error stays in a unique fixture.
$failureFixture = Join-Path ([IO.Path]::GetTempPath()) ('FrameForge-exit-check-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $failureFixture)
$failed = Start-Process -FilePath $exe -ArgumentList '--failure-exit-test' -WorkingDirectory $failureFixture -WindowStyle Hidden -PassThru
if (-not $failed.WaitForExit(30000)) { $failed.Kill(); throw 'Failure-exit fixture timed out' }
$expectedError = Join-Path $failureFixture 'artifacts/ui-test-error.txt'
if ($failed.ExitCode -ne 1 -or -not (Test-Path -LiteralPath $expectedError) -or -not (Get-Content -LiteralPath $expectedError -Raw).Contains('Synthetic automation failure')) { throw 'Automated failures did not preserve a failing exit code and diagnostic' }
'Passed: intentional automated UI failure returned exit code 1 with its diagnostic in an isolated temporary fixture.' | Set-Content -Encoding utf8 (Join-Path $qa 'failure-exit-ui.txt')
