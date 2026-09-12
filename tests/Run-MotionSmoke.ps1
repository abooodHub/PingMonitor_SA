param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
[void][Reflection.Assembly]::LoadFrom((Join-Path $root "bin\$Configuration\PingMonitor.SA.v1.exe"))
[Windows.Forms.Application]::EnableVisualStyles()
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
function Read-Field($object, $name) { return ,($object.GetType().GetField($name, $flags).GetValue($object)) }
function Set-Field($object, $name, $value) { $object.GetType().GetField($name, $flags).SetValue($object, $value) }
function Invoke-Event($object, $name, $argument) { $object.GetType().GetMethod($name, $flags).Invoke($object, @($argument)) }
function Pump([int]$milliseconds) {
    $clock = [Diagnostics.Stopwatch]::StartNew()
    while ($clock.ElapsedMilliseconds -lt $milliseconds) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 5 }
}
function Check([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
$testPath = Join-Path $PSScriptRoot ('.motion-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testPath)
$form = $null
try {
    $store = [PingMonitor.MonitoringListStore]::new((Join-Path $testPath 'lists.xml'))
    $form = [PingMonitor.Form1]::new($store)
    Set-Field $form 'updating' $false
    (Read-Field $form 'pingTimer').Stop()
    $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual
    $form.Location = [Drawing.Point]::new(-32000, -32000)
    $form.ShowInTaskbar = $false
    $form.Show()
    Pump 350
    Check ([Math]::Abs($form.Opacity - 1) -lt .001) 'Window fade did not complete.'
    $button = $form.Controls.Find('btnChangeInterval', $true)[0]
    Invoke-Event $button 'OnMouseEnter' ([EventArgs]::Empty)
    Pump 90
    $first = Read-Field $button 'sheenProgress'
    Pump 100
    Check ($first -gt 0 -and $first -lt (Read-Field $button 'sheenProgress')) 'Hover sheen did not advance over time.'
    Pump 850
    Check (-not (Read-Field $button 'animationTimer').Enabled) 'Button timer kept running after hover settled.'
    Invoke-Event $button 'OnMouseDown' ([Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, 25, 18, 0))
    Pump 80
    Check ((Read-Field $button 'rippleProgress') -gt 0 -and (Read-Field $button 'rippleProgress') -lt 1) 'Click ripple did not animate.'
    Invoke-Event $button 'OnMouseCaptureChanged' ([EventArgs]::Empty)
    Pump 850
    Check (-not (Read-Field $button 'animationTimer').Enabled) 'Click animation failed to stop.'

    $table = $form.Controls.Find('serverListView', $true)[0]
    $row = $table.Items[0]
    $row.Selected = $true
    $point = [Drawing.Point]::new($row.Bounds.Left + 20, $row.Bounds.Top + 5)
    Invoke-Event $table 'OnMouseMove' ([Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::None, 0, $point.X, $point.Y, 0))
    Pump 220
    Check ($table.GetRowBackground($row, $table.BackColor).ToArgb() -ne $table.BackColor.ToArgb()) 'Selected row has no visible highlight.'
    $metrics = [PingMonitor.ServerMetrics]::new(); $metrics.RecordSuccess(42)
    (Read-Field $form 'serverMetrics')[$row.Tag.ServerId] = $metrics
    $refresh = $form.GetType().GetMethod('RefreshMeasuredRows', $flags)
    $refresh.Invoke($form, @())
    Pump 650
    Check (-not (Read-Field $form 'metricPulseTimer').Enabled) 'Metric pulse timer never settled.'
    $refresh.Invoke($form, @())
    Check (-not (Read-Field $form 'metricPulseTimer').Enabled) 'Unchanged measurements were animated again.'

    $progress = $form.Controls.Find('scanProgress', $true)[0]
    $progress.SetProgress($true, 4, 10)
    Pump 180
    Check ((Read-Field $progress 'animationTimer').Enabled) 'Scan progress did not animate.'
    $form.WindowState = [Windows.Forms.FormWindowState]::Minimized
    Pump 100
    Check (-not (Read-Field $progress 'animationTimer').Enabled) 'Scan animation kept running while minimized.'
    $form.WindowState = [Windows.Forms.FormWindowState]::Normal
    Pump 120
    Check ((Read-Field $progress 'animationTimer').Enabled) 'Scan animation did not resume after restoring the window.'
    $progress.SetProgress($false, 10, 10)
    Pump 650
    Check (-not (Read-Field $progress 'animationTimer').Enabled) 'Completed scan animation kept running.'

    Invoke-Event $button 'OnMouseEnter' ([EventArgs]::Empty)
    $progress.SetProgress($true, 5, 10)
    (Read-Field $form 'reduceMotionMenu').Checked = $true
    Pump 60
    Check (-not (Read-Field $button 'animationTimer').Enabled -and -not (Read-Field $progress 'animationTimer').Enabled) 'Reduce motion did not stop animated controls.'
    [string[]]$ids = @([PingMonitor.ServerCatalog]::Create() | ForEach-Object Id)
    Check ($store.Load($ids).ReduceMotion) 'Reduced motion preference was not saved.'
    Check ($form.Controls.Find('onlyUnresponsive', $true).Count -eq 0) 'The removed no-reply filter returned.'
    $form.Dispose(); $form = $null
    Pump 100
    $effects = [PingMonitor.Form1].Assembly.GetType('PingMonitor.UiEffects')
    $listeners = $effects.GetField('MotionPreferenceChanged', [Reflection.BindingFlags]'Static,NonPublic').GetValue($null)
    Check ($null -eq $listeners -or $listeners.GetInvocationList().Length -eq 0) 'Animation event subscriptions leaked after closing the form.'
    Write-Output 'PASS: fade, hover, ripple, selection, one pulse per result, progress lifecycle, minimize/restore, reduced motion persistence and disposal.'
}
finally {
    if ($form) { $form.Dispose() }
    $resolved = [IO.Path]::GetFullPath($testPath)
    $prefix = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\') + '\.motion-'
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected cleanup path.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
