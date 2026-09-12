param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Windows.Forms
$root = Split-Path -Parent $PSScriptRoot
[void][Reflection.Assembly]::LoadFrom((Join-Path $root "bin\$Configuration\PingMonitor.SA.v1.exe"))
$testPath = Join-Path $root ('artifacts\features-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testPath)
function Check([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Reject([scriptblock]$action) { $rejected = $false; try { & $action } catch { $rejected = $true }; Check $rejected 'Invalid import was accepted.' }
$catalog = [PingMonitor.ServerCatalog]::Create()
$lists = [Collections.Generic.List[PingMonitor.MonitoringListDefinition]]::new()
$list = [PingMonitor.MonitoringListDefinition]::new(); $list.Id = 'first'; $list.Name = 'My list'; $list.ServerIds.Add($catalog[0].Id); $lists.Add($list)
$file = Join-Path $testPath 'export.xml'
[PingMonitor.MonitoringListTransfer]::Export($file, $lists)
$preview = [PingMonitor.MonitoringListTransfer]::Read($file, $catalog, $lists)
Check ($preview.Lists.Count -eq 1 -and $preview.Lists[0].Name -eq 'My list (2)' -and $preview.Lists[0].Id -ne 'first') 'Duplicate name/identity handling failed.'
Check ($preview.Lists[0].ServerIds[0] -eq $catalog[0].Id -and $lists[0].Name -eq 'My list') 'Round-trip changed membership or mutated source.'
[IO.File]::WriteAllText($file, '<PingMonitorLists version="1"><List name="Other"><Server>unknown</Server></List></PingMonitorLists>')
$preview = [PingMonitor.MonitoringListTransfer]::Read($file, $catalog, $lists)
Check ($preview.UnknownServers -eq 1 -and $preview.Lists[0].ServerIds.Count -eq 0) 'Unknown ID was not disclosed and omitted.'
$legacy = @([PingMonitor.ServerCatalog]::GetLegacyIdMap().GetEnumerator())[0]
[IO.File]::WriteAllText($file, ('<PingMonitorLists version="1"><List name="Migrated"><Server>' + $legacy.Key + '</Server></List></PingMonitorLists>'))
$preview = [PingMonitor.MonitoringListTransfer]::Read($file, $catalog, $lists)
Check ($preview.Lists[0].ServerIds[0] -eq $legacy.Value) 'Legacy import alias was not migrated.'
foreach ($invalid in @('<broken>', '<PingMonitorLists version="999"/>', '<!DOCTYPE x [<!ENTITY e SYSTEM "file:///missing">]><PingMonitorLists version="1"><List name="x"><Server>&e;</Server></List></PingMonitorLists>', '<PingMonitorLists version="1"><List name=""/></PingMonitorLists>')) {
    [IO.File]::WriteAllText($file, $invalid); Reject { [PingMonitor.MonitoringListTransfer]::Read($file, $catalog, $lists) }
}
[IO.File]::WriteAllText($file, (' ' * (1024 * 1024 + 1)))
Reject { [PingMonitor.MonitoringListTransfer]::Read($file, $catalog, $lists) }
[PingMonitor.MonitoringListTransfer]::Export($file, $lists)
$old = [IO.File]::ReadAllBytes($file)
$lock = [IO.File]::Open($file, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try { Reject { [PingMonitor.MonitoringListTransfer]::Export($file, $lists) } } finally { $lock.Dispose() }
Check ([Convert]::ToBase64String($old) -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))) 'Failed export damaged the old file.'
Check (@(Get-ChildItem $testPath -Filter '*.tmp').Count -eq 0) 'Export leaked temporary files.'
$metrics = [PingMonitor.ServerMetrics]::new()
$metrics.RecordFailure(); $metrics.RecordFailure(); $metrics.RecordError('DNS'); $metrics.RecordFailure()
Check ($metrics.UnresponsiveEpisodes -eq 0) 'Errors joined nonconsecutive failures.'
$metrics.RecordFailure(); $metrics.RecordFailure(); $metrics.RecordFailure()
Check ($metrics.UnresponsiveEpisodes -eq 1) 'Repeated failure produced repeated episodes.'
$metrics.RecordSuccess(12); $metrics.RecordSuccess(80)
1..3 | ForEach-Object { $metrics.RecordFailure() }
1..200 | ForEach-Object { $metrics.RecordSuccess(30) }
Check ($metrics.UnresponsiveEpisodes -eq 2 -and $metrics.SessionReplies -eq 202 -and $metrics.SessionFailures -eq 9 -and $metrics.SessionErrors -eq 1) 'Session totals are wrong.'
Check ($metrics.GetHistory().Count -eq 180 -and $metrics.SessionMinimum -eq 12 -and $metrics.SessionMaximum -eq 80) 'Totals expired with history or history grew unbounded.'
$map = [Collections.Generic.Dictionary[string, PingMonitor.ServerMetrics]]::new(); $map.Add($catalog[0].Id, $metrics)
$csv = [PingMonitor.SessionReport]::BuildCsv($catalog, $map)
Check ($csv.Contains('202,9,1,2,12,80,4.27')) 'Session CSV does not reflect full-session totals.'
$state = [PingMonitor.MonitoringListsState]::new()
Check (-not $state.AlertsEnabled) 'Alerts must default to disabled.'
$state.AlertsEnabled = $true
$store = [PingMonitor.MonitoringListStore]::new((Join-Path $testPath 'state.xml')); $store.Save($state)
Check ($store.Load([string[]]$catalog.Id).AlertsEnabled) 'Alert preference did not persist.'
Write-Output 'PASS: list round-trip, duplicate preview, aliases, unknown IDs, invalid/oversize/DTD rejection, atomic export, session totals, episode suppression and alert persistence.'
