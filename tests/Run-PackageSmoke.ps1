param([Parameter(Mandatory = $true)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Windows.Forms
$root = Split-Path -Parent $PSScriptRoot
$isolated = Join-Path $root ('artifacts\package-smoke-' + [Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $PackagePath -DestinationPath $isolated
$executables = @(Get-ChildItem -LiteralPath $isolated -Recurse -Filter 'PingMonitor.SA.v1.exe')
if ($executables.Count -ne 1) { throw 'Expected exactly one executable.' }
$exe = $executables[0].FullName
if (-not (Test-Path -LiteralPath ($exe + '.config'))) { throw 'Missing runtime configuration.' }
[xml]$config = Get-Content -LiteralPath ($exe + '.config')
if ($config.configuration.'System.Windows.Forms.ApplicationConfigurationSection'.add.value -ne 'PerMonitorV2') { throw 'Missing DPI configuration.' }
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
Write-Output ('Elevated process: ' + $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
[void][Reflection.Assembly]::LoadFrom($exe)
$data = Join-Path $isolated 'isolated-user-data\lists.xml'
$store = [PingMonitor.MonitoringListStore]::new($data)
$state = [PingMonitor.MonitoringListsState]::new(); $state.Language = 'en'; $state.ReduceMotion = $true
$list = [PingMonitor.MonitoringListDefinition]::new(); $list.Id = 'smoke'; $list.Name = 'Package smoke'; $list.ServerIds.Add([PingMonitor.ServerCatalog]::Create()[0].Id); $state.Lists.Add($list)
$store.Save($state)
$form = [PingMonitor.Form1]::new($store)
try {
    $flags = [Reflection.BindingFlags]'Instance,NonPublic'
    $form.GetType().GetField('updating', $flags).SetValue($form, $false)
    $form.GetType().GetField('pingTimer', $flags).GetValue($form).Stop()
    $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual; $form.Location = [Drawing.Point]::new(-32000,-32000); $form.ShowInTaskbar = $false
    $form.Show(); [Windows.Forms.Application]::DoEvents()
    if ($form.Controls.Find('cmbMonitorList', $true)[0].Items.Count -ne 2) { throw 'Packaged application did not load isolated lists.' }
    if ($store.Load([string[]][PingMonitor.ServerCatalog]::Create().Id).Lists.Count -ne 1) { throw 'Packaged storage round-trip failed.' }
}
finally { $form.Dispose() }
Write-Output 'PASS: extracted runtime assembly, configuration, WinForms resources and isolated user-data round-trip. This is not a fresh OS or real multi-monitor DPI test.'
