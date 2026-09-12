param([string]$Configuration = 'Release', [switch]$Capture)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
[void][Reflection.Assembly]::LoadFrom((Join-Path $root "bin\$Configuration\PingMonitor.SA.v1.exe"))
[Windows.Forms.Application]::EnableVisualStyles()
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
function Field($object, [string]$name) { return ,($object.GetType().GetField($name, $flags).GetValue($object)) }
function Set-Field($object, [string]$name, $value) { $object.GetType().GetField($name, $flags).SetValue($object, $value) }
function Invoke-Private($object, [string]$name, [object[]]$arguments = @()) { $object.GetType().GetMethod($name, $flags).Invoke($object, $arguments) }
function Check([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Snap($form, [string]$name) {
    $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual
    $form.Location = [Drawing.Point]::new(-32000, -32000)
    $form.ShowInTaskbar = $false
    $form.Show()
    [Windows.Forms.Application]::DoEvents()
    $settle = [Diagnostics.Stopwatch]::StartNew()
    while ($settle.ElapsedMilliseconds -lt 650) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 5 }
    $bitmap = [Drawing.Bitmap]::new($form.Width, $form.Height)
    try { $form.DrawToBitmap($bitmap, [Drawing.Rectangle]::new(0,0,$form.Width,$form.Height)); $bitmap.Save((Join-Path $PSScriptRoot $name)) }
    finally { $bitmap.Dispose(); $form.Hide() }
}
$testPath = Join-Path $PSScriptRoot ('.regression-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testPath)
$main = $null
$dialog = $null
try {
    $catalog = [PingMonitor.ServerCatalog]::Create()
    Check (@($catalog.Host | Select-Object -Unique).Count -eq $catalog.Count) 'Duplicate hosts remain.'
    Check (@($catalog | Where-Object ProviderId -eq 'google-cloud').Count -eq 0) 'Mislabelled Google endpoints remain.'
    $store = [PingMonitor.MonitoringListStore]::new((Join-Path $testPath 'lists.xml'))
    $state = [PingMonitor.MonitoringListsState]::new()
    $a = [PingMonitor.MonitoringListDefinition]::new(); $a.Id = 'a'; $a.Name = 'A'; $a.ServerIds.Add($catalog[0].Id)
    $b = [PingMonitor.MonitoringListDefinition]::new(); $b.Id = 'b'; $b.Name = 'B'; $b.ServerIds.Add($catalog[1].Id)
    $legacy = [PingMonitor.MonitoringListDefinition]::new(); $legacy.Id = 'legacy'; $legacy.Name = 'Legacy'
    $aliases = [PingMonitor.ServerCatalog]::GetLegacyIdMap()
    foreach ($id in $aliases.Keys) { $legacy.ServerIds.Add($id) }
    $state.Lists.Add($a); $state.Lists.Add($b); $state.Lists.Add($legacy)
    $state.ActiveListId = 'legacy'; $state.Language = 'en'; $state.SortColumn = 4; $state.SortDescending = $true
    $store.Save($state)
    [string[]]$ids = @($catalog | ForEach-Object Id)
    $loaded = $store.Load($ids, $aliases)
    Check ($loaded.Lists[2].ServerIds.Count -eq 6) 'Legacy migration dropped members.'
    foreach ($id in $loaded.Lists[2].ServerIds) { Check ($ids -contains $id) 'Invalid migrated server ID.' }
    Check ($loaded.ActiveListId -eq 'legacy' -and $loaded.Language -eq 'en') 'Active list/preferences not retained.'
    $store.Save($loaded)
    [IO.File]::WriteAllText((Join-Path $testPath 'lists.xml'), '<broken>')
    $recovered = $store.Load($ids, $aliases)
    Check ($recovered.Lists.Count -eq 3) 'Backup recovery failed.'
    $store.Save($recovered)
    [IO.File]::WriteAllText((Join-Path $testPath 'lists.xml'), '<broken-again>')
    Check ($store.Load($ids, $aliases).Lists.Count -eq 3) 'Recovery overwrote the good backup.'
    $store.Save($loaded)
    $locked = [IO.File]::Open((Join-Path $testPath 'lists.xml'), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $saveFailed = $false
    try { try { $store.Save($loaded) } catch { $saveFailed = $true } } finally { $locked.Dispose() }
    Check ($saveFailed -and $store.Load($ids, $aliases).Lists.Count -eq 3) 'Failed atomic save damaged existing lists.'
    Check (@(Get-ChildItem -LiteralPath $testPath -Filter '*.tmp').Count -eq 0) 'Failed save left temporary files.'
    Write-Output 'PASS: canonical catalog, legacy membership migration, preferences, repeated backup recovery.'

    [Func[PingMonitor.ServerDefinition,string]]$country = { param($s) $s.CountryCode }
    [Func[PingMonitor.ServerDefinition,string]]$location = { param($s) $s.LocationAr }
    [Func[PingMonitor.ServerDefinition,string]]$provider = { param($s) $s.ProviderName }
    $dialog = [PingMonitor.MonitoringListsForm]::new($catalog, $loaded.Lists, 'en', $country, $location, $provider, $null, $null)
    $null = $dialog.Handle
    $box = Field $dialog 'listsBox'; $view = Field $dialog 'serversView'; $null = $view.Handle
    $box.SelectedIndex = 1
    Check ($view.CheckedItems.Count -eq 1 -and $view.CheckedItems[0].Tag -eq $catalog[1].Id) 'Switching to B copied A selections.'
    $box.SelectedIndex = 0
    Check ($dialog.ResultLists[1].ServerIds[0] -eq $catalog[1].Id) 'B was corrupted after switching back.'
    $search = Field $dialog 'txtSearch'; $search.Text = 'not-a-matching-host'
    Check ($view.Items.Count -eq 0) 'List search did not filter.'
    $box.SelectedIndex = 1; $search.Clear()
    Check ($view.CheckedItems.Count -eq 1 -and $view.CheckedItems[0].Tag -eq $catalog[1].Id) 'Filtered list switch lost membership.'
    $fresh = [PingMonitor.MonitoringListDefinition]::new(); $fresh.Id = 'new'; $fresh.Name = 'Empty'
    (Field $dialog 'workingLists').Add($fresh)
    Invoke-Private $dialog 'RefreshLists' @('new')
    Check ($view.CheckedItems.Count -eq 0) 'New list inherited old selections.'
    $view.Items[0].Checked = $true
    $renameTimer = [Windows.Forms.Timer]::new(); $renameTimer.Interval = 20
    $renameTimer.Add_Tick({
        foreach ($openForm in @([Windows.Forms.Application]::OpenForms)) {
            if ($openForm.Text -eq 'List name') {
                $inputs = @($openForm.Controls | Where-Object { $_ -is [Windows.Forms.TextBox] })
                $inputs[0].Text = 'Renamed with selection'
                $openForm.DialogResult = [Windows.Forms.DialogResult]::OK
                $openForm.Close()
                $renameTimer.Stop()
                break
            }
        }
    })
    $renameTimer.Start()
    try { Invoke-Private $dialog 'BtnRename_Click' @($null, [EventArgs]::Empty) } finally { $renameTimer.Dispose() }
    Check ($view.CheckedItems.Count -eq 1 -and $dialog.ResultLists[3].Name -eq 'Renamed with selection') 'Rename dropped unsaved selections.'
    $dialog.Dispose(); $dialog = $null
    Write-Output 'PASS: switching lists, switching while filtered, and empty new-list regression.'

    $metrics = [PingMonitor.ServerMetrics]::new()
    $metrics.RecordSuccess(10); $metrics.RecordSuccess(20); $metrics.RecordFailure('TimedOut'); $metrics.RecordSuccess(100)
    Check ($metrics.JitterMilliseconds -eq 10 -and $metrics.LossPercent -eq 25) 'Jitter crossed a failed sample.'
    $metrics.RecordError('DNS error')
    Check ($metrics.SampleCount -eq 4 -and $metrics.LossPercent -eq 25 -and $metrics.IsMeasurementError) 'Local errors inflated packet loss.'
    1..200 | ForEach-Object { $metrics.RecordSuccess(30) }
    Check ($metrics.SampleCount -eq 20 -and $metrics.GetHistory().Length -eq 180) 'Metrics windows are not bounded.'
    Check ($metrics.LossPercent -eq 0 -and $metrics.JitterMilliseconds -eq 0 -and $metrics.AverageMilliseconds -eq 30) 'Old samples did not expire.'
    $csv = [PingMonitor.ServerDetailsForm]::BuildCsv('=formula,"test"', $metrics.GetHistory())
    Check ($csv.Contains('TimestampUtc,Host,Protocol') -and $csv.Contains("'=formula,")) 'CSV escaping/formula neutralization failed.'
    Write-Output 'PASS: loss/jitter windows, measurement errors, bounded history and CSV encoding.'

    $loaded.ActiveListId = 'all'; $loaded.Language = 'ar'; $loaded.SortColumn = -1
    $store.Save($loaded)
    $main = [PingMonitor.Form1]::new($store)
    Set-Field $main 'updating' $false
    (Field $main 'pingTimer').Stop()
    $null = $main.Handle
    $quick = $main.Controls.Find('txtQuickSearch', $true)[0]
    $table = $main.Controls.Find('serverListView', $true)[0]
    $targetsBefore = @(Invoke-Private $main 'GetMonitoredServers').Count
    $quick.Text = 'no-such-host'
    Check ($table.Items.Count -eq 0) 'Main search did not hide rows.'
    Check (@(Invoke-Private $main 'GetMonitoredServers').Count -eq $targetsBefore) 'Search changed monitoring targets.'
    $quick.Clear()
    $dictionary = Field $main 'serverMetrics'
    $m1 = [PingMonitor.ServerMetrics]::new(); $m1.RecordSuccess(150)
    $m2 = [PingMonitor.ServerMetrics]::new(); $m2.RecordSuccess(20)
    $dictionary[$catalog[0].Id] = $m1; $dictionary[$catalog[1].Id] = $m2
    $up = [PingMonitor.ServerRowComparer]::new(4, $false, $dictionary)
    $down = [PingMonitor.ServerRowComparer]::new(4, $true, $dictionary)
    $row1 = [Windows.Forms.ListViewItem]::new(); $row1.Tag = [PingMonitor.ServerRowTag]::new($catalog[0].Id, $catalog[0].Host)
    $row2 = [Windows.Forms.ListViewItem]::new(); $row2.Tag = [PingMonitor.ServerRowTag]::new($catalog[1].Id, $catalog[1].Host)
    $empty = [Windows.Forms.ListViewItem]::new(); $empty.Tag = [PingMonitor.ServerRowTag]::new($catalog[2].Id, $catalog[2].Host)
    Check ($up.Compare($row1,$row2) -gt 0 -and $down.Compare($row1,$row2) -lt 0) 'Numeric sort direction is wrong.'
    Check ($up.Compare($empty,$row1) -gt 0 -and $down.Compare($empty,$row1) -gt 0) 'Missing values must stay last.'
    $m2.RecordFailure('TimedOut')
    Invoke-Private $main 'btnToggleLang_Click' @($null, [EventArgs]::Empty)
    Check ($store.Load($ids, $aliases).Language -eq 'en') 'Language was not persisted.'
    if ($Capture) {
        Snap $main 'updated-ui-en.png'
        Invoke-Private $main 'btnToggleLang_Click' @($null, [EventArgs]::Empty)
        Snap $main 'updated-ui-ar.png'
        $sampleMetrics = [PingMonitor.ServerMetrics]::new()
        1..60 | ForEach-Object { if ($_ % 17 -eq 0) { $sampleMetrics.RecordFailure('TimedOut') } else { $sampleMetrics.RecordSuccess(25 + ($_ % 8) * 4) } }
        $history = $sampleMetrics.GetHistory()
        for ($i=0; $i -lt $history.Length; $i++) { $history[$i].TimestampUtc = [DateTime]::UtcNow.AddSeconds(-5 * ($history.Length - $i)) }
        [Func[PingMonitor.ServerMetrics]]$read = { $sampleMetrics }
        $details = [PingMonitor.ServerDetailsForm]::new($catalog[0], 'ar', 'Preview / بيانات تجريبية', $read)
        try { Snap $details 'updated-details-demo.png' } finally { $details.Dispose() }
    }
    Write-Output 'PASS: display-only search, numeric sorting, missing values and language persistence.'

    $method = [PingMonitor.Form1].GetMethod('MeasureServerAsync', [Reflection.BindingFlags]'Static,NonPublic')
    $gate = [Threading.SemaphoreSlim]::new(1,1)
    try {
        $target = [PingMonitor.PingTarget]::new(); $target.ServerId = 'loopback'; $target.Host = '127.0.0.1'
        $task = $method.Invoke($null, @($target, $gate, [Threading.CancellationToken]::None))
        $deadline = [DateTime]::UtcNow.AddSeconds(5)
        while (-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 5 }
        Check ($task.IsCompleted) 'Loopback measurement exceeded timeout.'
        Check ($task.Result.IsSuccess) 'Loopback ICMP did not reply.'
        $cancel = [Threading.CancellationTokenSource]::new(); $cancel.Cancel()
        try {
            $task = $method.Invoke($null, @($target, $gate, $cancel.Token))
            $deadline = [DateTime]::UtcNow.AddSeconds(5)
            while (-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 5 }
            Check ($task.IsCanceled) 'Cancelled measurement was treated as failure.'
            Check ($gate.CurrentCount -eq 1) 'Cancellation leaked a semaphore slot.'
        } finally { $cancel.Dispose() }
    } finally { $gate.Dispose() }
    Write-Output 'PASS: actual loopback ICMP, cancellation and semaphore cleanup.'

    # Exercise the actual UI scan scheduler against loopback only, with a message pump.
    $localTargets = [Collections.Generic.List[PingMonitor.ServerDefinition]]::new()
    1..24 | ForEach-Object { $localTargets.Add([PingMonitor.ServerDefinition]::new('sa', ('local-' + $_), 'STC', '127.0.0.1')) }
    Set-Field $main 'serverDefinitions' $localTargets
    $dictionary.Clear()
    Invoke-Private $main 'PopulateListView'
    Set-Field $main 'updateInterval' 60000
    Set-Field $main 'updating' $true
    $scan = Invoke-Private $main 'RunPingCycleAsync'
    Invoke-Private $main 'btnToggleTimer_Click' @($null, [EventArgs]::Empty)
    $before = $dictionary.Count
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not $scan.IsCompleted -and [DateTime]::UtcNow -lt $deadline) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 5 }
    Check ($scan.IsCompleted -and -not $scan.IsFaulted) 'Cancelled scan did not finish cleanly.'
    Check ($dictionary.Count -eq $before -and -not (Field $main 'pingTimer').Enabled) 'Pause accepted late results or restarted the timer.'
    Invoke-Private $main 'btnToggleTimer_Click' @($null, [EventArgs]::Empty)
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (($dictionary.Count -ne 24 -or (Field $main 'scanInProgress')) -and [DateTime]::UtcNow -lt $deadline) {
        [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 5
    }
    Check ($dictionary.Count -eq 24 -and -not (Field $main 'scanInProgress')) 'Resume did not measure all selected targets.'
    Check (@($dictionary.Values | Where-Object IsOnline).Count -eq 24) 'Loopback cycle reported a failure.'
    Check ((Field $main 'scanCompleted') -eq 24 -and (Field $main 'lastResultUtc')) 'Progress/last update did not refresh.'
    Invoke-Private $main 'btnToggleTimer_Click' @($null, [EventArgs]::Empty)
    Write-Output 'PASS: complete 24-target UI scan, pause, stale-result rejection, immediate resume and progress.'
}
finally {
    if ($dialog) { $dialog.Dispose() }
    if ($main) { $main.Dispose() }
    $resolved = [IO.Path]::GetFullPath($testPath)
    $prefix = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\') + '\.regression-'
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected cleanup path.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
