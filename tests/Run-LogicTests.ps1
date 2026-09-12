param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "$Message Expected=$Expected Actual=$Actual"
    }
}

function Assert-Near {
    param([double]$Expected, [double]$Actual, [double]$Tolerance, [string]$Message)
    if ([Math]::Abs($Expected - $Actual) -gt $Tolerance) {
        throw "$Message Expected=$Expected Actual=$Actual"
    }
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$assemblyPath = Join-Path $projectRoot "bin\$Configuration\PingMonitor.SA.v1.exe"
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw "Build output was not found: $assemblyPath"
}

[void][Reflection.Assembly]::LoadFrom($assemblyPath)

$servers = [PingMonitor.ServerCatalog]::Create()

Assert-Equal 59 $servers.Count 'Catalog count mismatch.'
$mobily = @($servers | Where-Object ProviderId -eq 'mobily')
$europe = @($servers | Where-Object RegionId -eq 'europe')
Assert-Equal 11 $mobily.Count 'Mobily count mismatch.'
Assert-Equal 8 $europe.Count 'Europe count mismatch.'

$combined = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($server in $mobily + $europe) { [void]$combined.Add($server.Id) }
Assert-Equal 19 $combined.Count 'Mobily + Europe union mismatch.'

$metrics = [PingMonitor.ServerMetrics]::new()
$metrics.RecordSuccess(10)
$metrics.RecordSuccess(20)
$metrics.RecordFailure()
Assert-Near 33.333 $metrics.LossPercent 0.01 'Loss calculation mismatch.'
Assert-Near 10 $metrics.JitterMilliseconds 0.01 'Jitter calculation mismatch.'

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('PingMonitor.Tests.' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
try {
    $storagePath = Join-Path $testRoot 'server-lists.v1.xml'
    $store = [PingMonitor.MonitoringListStore]::new($storagePath)
    $state = [PingMonitor.MonitoringListsState]::new()
    $list = [PingMonitor.MonitoringListDefinition]::new()
    $list.Id = [Guid]::NewGuid().ToString('N')
    $list.Name = 'Mobily + Europe'
    foreach ($id in $combined) { $list.ServerIds.Add($id) }
    $state.Lists.Add($list)
    $state.ActiveListId = $list.Id
    $store.Save($state)

    [string[]]$validIds = @($servers | ForEach-Object Id)
    $loaded = $store.Load($validIds)
    Assert-Equal 1 $loaded.Lists.Count 'Stored list count mismatch.'
    Assert-Equal 19 $loaded.Lists[0].ServerIds.Count 'Stored server membership mismatch.'
    Assert-Equal $list.Id $loaded.ActiveListId 'Active list was not restored.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        if (-not [IO.Path]::GetFullPath($testRoot).StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()) + "PingMonitor.Tests.", [StringComparison]::OrdinalIgnoreCase)) { throw "Unexpected test path" }
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}

Write-Output 'PASS: catalog=59, mobily=11, europe=8, union=19, metrics and persistence verified.'
