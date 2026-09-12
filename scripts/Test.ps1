param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release', [ValidateSet('Full', 'CI')][string]$Profile = 'Full')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
$windowsPowerShell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
$suites = @('Run-LogicTests', 'Run-UiSmoke', 'Run-RegressionTests', 'Run-MotionSmoke', 'Run-ReleaseFeaturesTests', 'Run-LayoutTests')
if ($Profile -eq 'Full') { $suites += 'Run-ProviderSwitchTests' }
else { Write-Host 'SKIP: ProviderSwitch live pending-network test requires the Full profile on a suitable network.' }
foreach ($suite in $suites) {
    Write-Host "Running $suite..."
    & $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -Sta -File (Join-Path $projectRoot "tests\$suite.ps1") -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw "$suite failed (exit $LASTEXITCODE)." }
}
Write-Host "All $($suites.Count) test suites in $Profile profile passed."
