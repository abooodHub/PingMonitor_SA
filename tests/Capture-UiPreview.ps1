param(
    [string]$Configuration = 'Release',
    [string]$OutputPath = '.\tests\ui-preview.png'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$assemblyPath = Join-Path $projectRoot "bin\$Configuration\PingMonitor.SA.v1.exe"
[void][Reflection.Assembly]::LoadFrom($assemblyPath)

$form = [PingMonitor.Form1]::new()
$form.GetType().GetField("updating", [Reflection.BindingFlags]"Instance,NonPublic").SetValue($form, $false)
try {
    $timerField = $form.GetType().GetField('pingTimer', [Reflection.BindingFlags]'Instance,NonPublic')
    $timerField.GetValue($form).Stop()
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
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedOutput)) | Out-Null
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
}
