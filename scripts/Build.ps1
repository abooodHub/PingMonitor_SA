param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$MSBuildPath,
    [ValidateSet('Full', 'CI')][string]$TestProfile = 'Full',
    [switch]$Test,
    [switch]$Package
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
if ($Package -and $Configuration -ne 'Release') { throw 'Packages require Release configuration.' }
if (-not $MSBuildPath) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $MSBuildPath = & $vswhere -latest -prerelease -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }
    if (-not $MSBuildPath) {
        $command = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
        if ($command) { $MSBuildPath = $command.Source }
    }
}
if (-not $MSBuildPath -or -not (Test-Path -LiteralPath $MSBuildPath)) {
    throw 'Install Visual Studio/Build Tools with .NET desktop development and .NET Framework 4.7.2 targeting pack, or pass -MSBuildPath.'
}
& $MSBuildPath (Join-Path $projectRoot 'PingMonitor.sln') /t:Rebuild "/p:Configuration=$Configuration" /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }
# Every distributable package must pass the test suites for this build.
if ($Test -or $Package) { & (Join-Path $PSScriptRoot 'Test.ps1') -Configuration $Configuration -Profile $TestProfile }
if (-not $Package) { return }

$outputPath = Join-Path $projectRoot "bin\$Configuration"
$exePath = Join-Path $outputPath 'PingMonitor.SA.v1.exe'
$fileVersion = [Version][Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).FileVersion
$version = '{0}.{1}.{2}' -f $fileVersion.Major, $fileVersion.Minor, $fileVersion.Build
$releasePath = Join-Path $projectRoot 'release'
$stagingPath = Join-Path $projectRoot ('artifacts\packages\' + [Guid]::NewGuid().ToString('N'))
$runtimePath = Join-Path $stagingPath "PingMonitor-SA-$version"
$sourcePath = Join-Path $stagingPath "PingMonitor-SA-$version-source"
foreach ($directory in @($releasePath, $runtimePath, $sourcePath)) { [void][IO.Directory]::CreateDirectory($directory) }
foreach ($file in @('PingMonitor.SA.v1.exe', 'PingMonitor.SA.v1.exe.config')) {
    Copy-Item -LiteralPath (Join-Path $outputPath $file) -Destination $runtimePath
}
Copy-Item -LiteralPath (Join-Path $projectRoot "docs\RELEASE-$version-AR.md") -Destination (Join-Path $runtimePath 'RELEASE-NOTES.md')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PACKAGE-README.txt') -Destination (Join-Path $runtimePath 'README.txt')
# Explicit source allowlist: never include local user data, output or upstream snapshots.
$sourceItems = @('.gitignore', '.github', 'app.manifest', 'App.config', 'Program.cs', 'ping.ico', 'PingMonitor.csproj', 'PingMonitor.sln', 'README.md', 'CHANGELOG.md', 'Core', 'Services', 'Persistence', 'UI', 'Properties', 'Resources', 'docs', 'scripts')
foreach ($item in $sourceItems) { Copy-Item -LiteralPath (Join-Path $projectRoot $item) -Destination $sourcePath -Recurse }
[void][IO.Directory]::CreateDirectory((Join-Path $sourcePath 'tests'))
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests') -Filter '*.ps1' -File | Copy-Item -Destination (Join-Path $sourcePath 'tests')
$runtimeZip = Join-Path $releasePath "PingMonitor-SA-$version.zip"
$sourceZip = Join-Path $releasePath "PingMonitor-SA-$version-source.zip"
Compress-Archive -LiteralPath $runtimePath -DestinationPath $runtimeZip -Force
Compress-Archive -LiteralPath $sourcePath -DestinationPath $sourceZip -Force
# Refresh the convenient unpacked local executable as well.
Copy-Item -LiteralPath $runtimePath -Destination $releasePath -Recurse -Force
$hashLines = foreach ($file in @($runtimeZip, $sourceZip, (Join-Path $releasePath "PingMonitor-SA-$version\PingMonitor.SA.v1.exe"))) {
    $hash = Get-FileHash -LiteralPath $file -Algorithm SHA256
    '{0}  {1}' -f $hash.Hash.ToLowerInvariant(), $file.Substring($releasePath.Length + 1).Replace('\', '/')
}
$hashLines | Set-Content -LiteralPath (Join-Path $releasePath "SHA256SUMS-$version.txt") -Encoding ascii
Write-Host "Packages and SHA256 checksums: $releasePath"
