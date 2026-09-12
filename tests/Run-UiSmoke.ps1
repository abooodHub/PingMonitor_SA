param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Windows.Forms

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$assemblyPath = Join-Path $projectRoot "bin\$Configuration\PingMonitor.SA.v1.exe"
[void][Reflection.Assembly]::LoadFrom($assemblyPath)
[Windows.Forms.Application]::EnableVisualStyles()

$form = [PingMonitor.Form1]::new()
try {
    $combo = $form.Controls.Find('cmbMonitorList', $true)[0]
    $manage = $form.Controls.Find('btnManageMonitorLists', $true)[0]
    $language = $form.Controls.Find('btnToggleLang', $true)[0]
    $serverView = $form.Controls.Find('serverListView', $true)[0]
    $providers = $form.Controls.Find('pnlProviders', $true)[0]

    Assert-True ($null -ne $combo) 'Monitor-list combo was not created.'
    Assert-True ($combo.Items.Count -ge 1) 'The All servers list is missing.'
    Assert-True ($null -ne $manage) 'Manage Lists button was not created.'
    Assert-True (-not $manage.Bounds.IntersectsWith($language.Bounds)) 'Manage Lists overlaps the language button.'
    Assert-True ($combo.Right -lt $manage.Left) 'Monitor-list combo overlaps Manage Lists.'
    Assert-True ($providers.Controls.Count -eq 7) 'Provider filters were not restored.'
    Assert-True ($serverView.Columns.Count -eq 7) 'Server table columns changed unexpectedly.'
    Assert-True ($serverView.Items.Count -gt 0) 'Server table is empty on first construction.'

    $catalogField = $form.GetType().GetField('serverDefinitions', [Reflection.BindingFlags]'Instance,NonPublic')
    $catalog = $catalogField.GetValue($form)
    Assert-True ($catalog.Count -eq 59) 'Internal server catalog count changed.'
}
finally {
    $form.Dispose()
}

Write-Output 'PASS: main UI constructed, 59-server catalog loaded, new controls fit the existing header.'
