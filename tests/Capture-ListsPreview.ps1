param(
    [string]$Configuration = 'Release',
    [string]$OutputPath = '.\tests\lists-preview.png'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$assemblyPath = Join-Path $projectRoot "bin\$Configuration\PingMonitor.SA.v1.exe"
[void][Reflection.Assembly]::LoadFrom($assemblyPath)

$catalog = [PingMonitor.ServerCatalog]::Create()
$sample = [PingMonitor.MonitoringListDefinition]::new()
$sample.Id = [Guid]::NewGuid().ToString('N')
$sample.Name = 'موبايلي + أوروبا'
foreach ($server in $catalog) {
    if ($server.ProviderId -eq 'mobily' -or $server.RegionId -eq 'europe') {
        $sample.ServerIds.Add($server.Id)
    }
}
$lists = [Collections.Generic.List[PingMonitor.MonitoringListDefinition]]::new()
$lists.Add($sample)

[Func[PingMonitor.ServerDefinition,string]]$countryText = { param($server) $server.CountryCode }
[Func[PingMonitor.ServerDefinition,string]]$locationText = { param($server) $server.LocationAr }
[Func[PingMonitor.ServerDefinition,string]]$providerText = { param($server) $server.ProviderName }

$mainForm = [PingMonitor.Form1]::new()
$mainForm.GetType().GetField("updating", [Reflection.BindingFlags]"Instance,NonPublic").SetValue($mainForm, $false)
$timerField = $mainForm.GetType().GetField('pingTimer', [Reflection.BindingFlags]'Instance,NonPublic')
$timerField.GetValue($mainForm).Stop()
$flagsField = $mainForm.GetType().GetField('imageListFlags', [Reflection.BindingFlags]'Instance,NonPublic')
$flags = $flagsField.GetValue($mainForm)
$form = [PingMonitor.MonitoringListsForm]::new($catalog, $lists, 'ar', $countryText, $locationText, $providerText, $flags, $mainForm.Icon)
try {
    $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual
    $form.Location = [Drawing.Point]::new(-32000, -32000)
    $form.ShowInTaskbar = $false
    $form.Show()
    [Windows.Forms.Application]::DoEvents()
    $form.PerformLayout()

    $bitmap = [Drawing.Bitmap]::new($form.Width, $form.Height)
    try {
        $bounds = [Drawing.Rectangle]::new(0, 0, $form.Width, $form.Height)
        $form.DrawToBitmap($bitmap, $bounds)
        $resolvedOutput = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputPath))
        $bitmap.Save($resolvedOutput, [Drawing.Imaging.ImageFormat]::Png)
        Write-Output $resolvedOutput
    }
    finally {
        $bitmap.Dispose()
    }
}
finally {
    $form.Hide()
    $form.Dispose()
    $mainForm.Dispose()
}
