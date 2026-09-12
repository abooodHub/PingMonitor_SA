param([string]$Configuration = 'Release', [switch]$Capture)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
[void][Reflection.Assembly]::LoadFrom((Join-Path $root "bin\$Configuration\PingMonitor.SA.v1.exe"))
[Windows.Forms.Application]::EnableVisualStyles()
$output = Join-Path $root 'artifacts\layout'
[void][IO.Directory]::CreateDirectory($output)
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
function Field($object, $name) { return ,($object.GetType().GetField($name, $flags).GetValue($object)) }
function SetField($object, $name, $value) { $object.GetType().GetField($name, $flags).SetValue($object, $value) }
function Check([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function ScaleFonts($control, [single]$scale) {
    foreach ($child in $control.Controls) { ScaleFonts $child $scale }
    $control.Font = [Drawing.Font]::new($control.Font.FontFamily, $control.Font.Size * $scale, $control.Font.Style)
}
foreach ($language in @('ar', 'en')) {
    foreach ($scale in @(1.0, 1.25, 1.5, 2.0)) {
        $state = [PingMonitor.MonitoringListsState]::new(); $state.Language = $language; $state.ReduceMotion = $true
        $store = [PingMonitor.MonitoringListStore]::new((Join-Path $output ('state-' + $language + '-' + $scale + '.xml'))); $store.Save($state)
        $form = [PingMonitor.Form1]::new($store)
        try {
            SetField $form 'updating' $false; (Field $form 'pingTimer').Stop()
            $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual
            $form.Location = [Drawing.Point]::new(-32000, -32000)
            $form.ShowInTaskbar = $false
            $form.Show()
            [Windows.Forms.Application]::DoEvents()
            $form.AutoScaleMode = [Windows.Forms.AutoScaleMode]::None
            SetField $form 'responsiveLayoutReady' $false
            $form.Scale([Drawing.SizeF]::new($scale, $scale)); ScaleFonts $form $scale
            $form.ClientSize = [Drawing.Size]::new([Math]::Min(1900, [int](1000 * $scale)), [int](555 * $scale))
            SetField $form 'responsiveLayoutReady' $true
            $form.GetType().GetMethod('ArrangeMainLayout', $flags).Invoke($form, @([single]$scale)) | Out-Null
            [Windows.Forms.Application]::DoEvents()
            $header = @('btnToggleIP','btnToggleTimer','btnChangeInterval','cmbMonitorList','btnManageMonitorLists','btnToggleLang','txtQuickSearch') | ForEach-Object { Field $form $_ }
            for ($i = 0; $i -lt $header.Count; $i++) {
                $control = $header[$i]
                Check ($control.Left -ge 0 -and $control.Right -le $form.ClientSize.Width) "$language $scale : clipped $($control.Name)"
                for ($j = $i + 1; $j -lt $header.Count; $j++) { Check (-not $control.Bounds.IntersectsWith($header[$j].Bounds)) "$language $scale : overlapping header" }
                if ($control -is [Windows.Forms.Button]) {
                    $textSize = [Windows.Forms.TextRenderer]::MeasureText($control.Text, $control.Font)
                    Check ($textSize.Width -le $control.Width -and $textSize.Height -le $control.Height) "$language $scale : truncated button $($control.Name) $($textSize.Width) / $($control.Width)"
                }
            }
            $providers = Field $form 'pnlProviders'; $table = Field $form 'serverListView'
            Check ((Field $form 'cmbMonitorList').ItemHeight -ge (Field $form 'cmbMonitorList').Font.Height) "$language $scale : clipped combo text height"
            Check ($providers.Bottom -lt $table.Top -and $table.Height -ge (50 * $scale)) "$language $scale : provider/table overlap or unusable table"
            if ($Capture) {
                $bitmap = [Drawing.Bitmap]::new($form.Width, $form.Height)
                try { $form.DrawToBitmap($bitmap, [Drawing.Rectangle]::new(0,0,$form.Width,$form.Height)); $bitmap.Save((Join-Path $output ("main-$language-$scale.png"))) } finally { $bitmap.Dispose() }
            }
            Write-Output "PASS: main $language layout at simulated $([int]($scale * 100)) percent."
        }
        finally { $form.Dispose() }
    }
}

# Exercise each secondary window with enlarged fonts and bounds, without changing OS DPI.
$catalog = [PingMonitor.ServerCatalog]::Create()
$lists = [Collections.Generic.List[PingMonitor.MonitoringListDefinition]]::new()
$list = [PingMonitor.MonitoringListDefinition]::new(); $list.Id = 'preview'; $list.Name = 'Mobily + Europe'; $list.ServerIds.Add($catalog[0].Id); $lists.Add($list)
[Func[PingMonitor.ServerDefinition,string]]$country = { param($s) $s.CountryCode }
[Func[PingMonitor.ServerDefinition,string]]$location = { param($s) $s.LocationAr }
[Func[PingMonitor.ServerDefinition,string]]$provider = { param($s) $s.ProviderName }
$metrics = [PingMonitor.ServerMetrics]::new(); $metrics.RecordSuccess(25)
[Func[PingMonitor.ServerMetrics]]$readMetrics = { $metrics }
foreach ($language in @('ar', 'en')) {
    foreach ($scale in @(1.0, 1.25, 1.5, 2.0)) {
        $forms = @(
            [PingMonitor.AboutForm]::new(($language -eq 'ar'), (Join-Path $output 'data.xml')),
            [PingMonitor.TextReviewForm]::new('Import preview', "Mobily + Europe - 19 servers`r`nAdds new lists without replacing existing lists.", 'Add lists', ($language -eq 'ar')),
            [PingMonitor.MonitoringListsForm]::new($catalog, $lists, $language, $country, $location, $provider, $null, $null),
            [PingMonitor.ServerDetailsForm]::new($catalog[0], $language, 'STC - Riyadh', $readMetrics)
        )
        foreach ($form in $forms) {
            try {
                $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual; $form.Location = [Drawing.Point]::new(-32000,-32000); $form.ShowInTaskbar = $false
                $form.Show(); [Windows.Forms.Application]::DoEvents()
                $form.AutoScaleMode = [Windows.Forms.AutoScaleMode]::None
                $form.Scale([Drawing.SizeF]::new($scale, $scale)); ScaleFonts $form $scale
                $form.PerformLayout(); [Windows.Forms.Application]::DoEvents()
                $queue = [Collections.Generic.Queue[Windows.Forms.Control]]::new(); $queue.Enqueue($form)
                while ($queue.Count -gt 0) {
                    $parent = $queue.Dequeue()
                    $buttons = @($parent.Controls | Where-Object { $_ -is [Windows.Forms.Button] })
                    foreach ($control in $parent.Controls) { if ($control.HasChildren) { $queue.Enqueue($control) } }
                    for ($i = 0; $i -lt $buttons.Count; $i++) {
                        $button = $buttons[$i]
                        Check ($button.Left -ge 0 -and $button.Right -le $parent.ClientSize.Width -and $button.Bottom -le $parent.ClientSize.Height) "$($form.GetType().Name) $language $scale : clipped button $($button.Text)"
                        $textSize = [Windows.Forms.TextRenderer]::MeasureText($button.Text, $button.Font)
                        Check ($textSize.Width -le $button.Width -and $textSize.Height -le $button.Height) "$($form.GetType().Name) $language $scale : truncated button $($button.Text)"
                        for ($j = $i + 1; $j -lt $buttons.Count; $j++) { Check (-not $button.Bounds.IntersectsWith($buttons[$j].Bounds)) 'Secondary dialog buttons overlap.' }
                    }
                }
                if ($Capture) {
                    $bitmap = [Drawing.Bitmap]::new($form.Width, $form.Height)
                    try { $form.DrawToBitmap($bitmap, [Drawing.Rectangle]::new(0,0,$form.Width,$form.Height)); $bitmap.Save((Join-Path $output ("$($form.GetType().Name)-$language-$scale.png"))) } finally { $bitmap.Dispose() }
                }
            }
            finally { $form.Dispose() }
        }
        Write-Output "PASS: four secondary dialogs $language at simulated $([int]($scale * 100)) percent."
    }
}
