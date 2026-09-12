param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
[void][Reflection.Assembly]::LoadFrom((Join-Path $root "bin\$Configuration\PingMonitor.SA.v1.exe"))
[Windows.Forms.Application]::EnableVisualStyles()
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
function Field($object, $name) { return ,($object.GetType().GetField($name, $flags).GetValue($object)) }
function Set-Field($object, $name, $value) { $object.GetType().GetField($name, $flags).SetValue($object, $value) }
function Invoke-Private($object, $name) { return ,($object.GetType().GetMethod($name, $flags).Invoke($object, @())) }
function Check([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Pump([int]$duration) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.ElapsedMilliseconds -lt $duration) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 5 }
}
$testPath = Join-Path $PSScriptRoot ('.provider-switch-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testPath)
$form = $null; $heartbeat = $null
try {
    $store = [PingMonitor.MonitoringListStore]::new((Join-Path $testPath 'lists.xml'))
    $form = [PingMonitor.Form1]::new($store)
    Set-Field $form 'updating' $false
    (Field $form 'pingTimer').Stop()
    $providers = Field $form 'pnlProviders'
    Set-Field $form 'suppressProviderEvents' $true
    foreach ($check in $providers.Controls) { $check.Checked = $false }
    Set-Field $form 'suppressProviderEvents' $false
    $targets = [Collections.Generic.List[PingMonitor.ServerDefinition]]::new()
    # TEST-NET-1 forces pending ICMP requests on the test network; no DNS dependency.
    1..8 | ForEach-Object { $targets.Add([PingMonitor.ServerDefinition]::new('sa', ('pending-' + $_), 'STC', '192.0.2.1')) }
    1..2 | ForEach-Object { $targets.Add([PingMonitor.ServerDefinition]::new('sa', ('loopback-' + $_), 'Mobily', '127.0.0.1')) }
    Set-Field $form 'serverDefinitions' $targets
    Set-Field $form 'updateInterval' 60000
    Invoke-Private $form 'PopulateListView' | Out-Null
    $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual
    $form.Location = [Drawing.Point]::new(-32000,-32000)
    $form.ShowInTaskbar = $false
    $form.Show()
    Pump 300
    $script:heartbeatCount = 0
    $script:lastBeat = 0L; $script:maxGap = 0L
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $heartbeat = [Windows.Forms.Timer]::new(); $heartbeat.Interval = 20
    $heartbeat.Add_Tick({
        $now = $clock.ElapsedMilliseconds
        if ($script:heartbeatCount -gt 0) { $script:maxGap = [Math]::Max($script:maxGap, $now - $script:lastBeat) }
        $script:lastBeat = $now; $script:heartbeatCount++
    })
    $heartbeat.Start()
    [Threading.SynchronizationContext]::SetSynchronizationContext([Windows.Forms.WindowsFormsSynchronizationContext]::new())
    Set-Field $form 'updating' $true
    $oldCycle = Invoke-Private $form 'RunPingCycleAsync'
    Pump 150
    Check (-not $oldCycle.IsCompleted) 'No pending requests; the network cannot exercise this regression.'
    $mobily = @($providers.Controls | Where-Object Tag -eq 'mobily')[0]
    $table = Field $form 'serverListView'
    $click = [Diagnostics.Stopwatch]::StartNew()
    $mobily.Checked = $true
    $form.Refresh()
    $click.Stop()
    # Keep settings isolated: the click schedules persistence; stop that timer before pumping.
    Check ((Field $form 'settingsSaveTimer').Enabled) 'Provider selection was not scheduled for deferred persistence.'
    (Field $form 'settingsSaveTimer').Stop()
    Check ($table.Items.Count -eq 2) 'The table did not immediately show the selected provider.'
    Check ($click.ElapsedMilliseconds -lt 500) ('Provider selection blocked for ' + $click.ElapsedMilliseconds + ' ms.')
    Check (@($table.Items | Where-Object { $_.Tag.Host -ne '127.0.0.1' }).Count -eq 0) 'Old provider rows survived the filter.'
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    $metrics = Field $form 'serverMetrics'
    while (($metrics.Count -ne 2 -or (Field $form 'scanInProgress')) -and [DateTime]::UtcNow -lt $deadline) { Pump 10 }
    Check ($oldCycle.IsCompleted -and -not $oldCycle.IsFaulted) 'Old cycle did not drain cleanly.'
    Check ($metrics.Count -eq 2 -and @( $metrics.Values | Where-Object IsOnline ).Count -eq 2) ('Replacement provider was not measured, or stale results were accepted. count=' + $metrics.Count + '; online=' + @( $metrics.Values | Where-Object IsOnline ).Count + '; scanning=' + (Field $form 'scanInProgress') + '; timer=' + (Field $form 'pingTimer').Enabled + '; completed=' + (Field $form 'scanCompleted') + '; total=' + (Field $form 'scanTotal'))
    Check ($script:heartbeatCount -gt 2 -and $script:maxGap -lt 500) ('UI heartbeat was blocked; max gap=' + $script:maxGap)
    Set-Field $form 'updating' $false
    (Field $form 'pingTimer').Stop()
    $stc = @($providers.Controls | Where-Object Tag -eq 'stc')[0]
    1..10 | ForEach-Object { $stc.Checked = $true; $mobily.Checked = $false; $mobily.Checked = $true; $stc.Checked = $false }
    (Field $form 'settingsSaveTimer').Stop()
    Check ($table.Items.Count -eq 2) 'Rapid switching did not keep the final selection.'
    Write-Output ('PASS: provider click + table repaint=' + $click.ElapsedMilliseconds + ' ms; UI heartbeat max gap=' + $script:maxGap + ' ms; pending scan cancelled, 2 replacement results, rapid switching verified.')
}
finally {
    if ($heartbeat) { $heartbeat.Dispose() }
    if ($form) { $form.Dispose() }
    $resolved = [IO.Path]::GetFullPath($testPath)
    $prefix = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\') + '\.provider-switch-'
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected cleanup path.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
