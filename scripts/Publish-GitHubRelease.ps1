$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
$version = [Version][Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $root 'bin\Release\PingMonitor.SA.v1.exe')).FileVersion
$number = '{0}.{1}.{2}' -f $version.Major, $version.Minor, $version.Build
$tag = "v$number"
if (-not $env:GITHUB_SHA -or $env:GH_REPO -ne 'abooodHub/PingMonitor_SA') { throw 'Run this publisher from the repository GitHub Actions workflow.' }
$existing = & gh release list --limit 100 --json tagName,isDraft | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Could not inspect existing releases.' }
$matching = @($existing | Where-Object tagName -eq $tag)
if ($matching.Count -gt 0) {
    if ($matching[0].isDraft) { throw "An unfinished draft for $tag already exists; inspect it before retrying publication." }
    Write-Host "$tag is already published; existing assets are preserved."
    return
}
$upload = Join-Path $root 'artifacts\release-upload'
[void][IO.Directory]::CreateDirectory($upload)
Copy-Item -LiteralPath (Join-Path $root "release\PingMonitor-SA-$number.zip") -Destination (Join-Path $upload 'PingMonitor.SA.v1.zip')
Copy-Item -LiteralPath (Join-Path $root "release\PingMonitor-SA-$number-source.zip") -Destination $upload
foreach ($name in @('PingMonitor.SA.v1.exe', 'PingMonitor.SA.v1.exe.config')) { Copy-Item -LiteralPath (Join-Path $root "bin\Release\$name") -Destination $upload }
$hashes = Get-ChildItem -LiteralPath $upload -File | ForEach-Object { '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name }
$hashes | Set-Content -LiteralPath (Join-Path $upload 'SHA256SUMS.txt') -Encoding ascii
$assets = @(Get-ChildItem -LiteralPath $upload -File | ForEach-Object FullName)
& gh release create $tag --target $env:GITHUB_SHA --title "PingMonitor.SA.v1 - $number" --notes-file (Join-Path $root "docs\GITHUB-RELEASE-$number.md") --draft @assets
if ($LASTEXITCODE -ne 0) { throw 'Release upload failed; the release is not published.' }
& gh release edit $tag --draft=false --latest
if ($LASTEXITCODE -ne 0) { throw 'Assets uploaded but publication failed.' }
