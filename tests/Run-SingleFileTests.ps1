param([string]$Configuration='Release', [string]$ExecutablePath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=Split-Path -Parent $PSScriptRoot
$work=Join-Path $root ('artifacts\single-file-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($work)
$exe=Join-Path $work 'PingMonitor.SA.v1.exe'
if (-not $ExecutablePath) { $ExecutablePath=Join-Path $root "bin\$Configuration\PingMonitor.SA.v1.exe" }
Copy-Item -LiteralPath $ExecutablePath -Destination $exe
$helper=Join-Path $work 'RuntimeProbe.exe'
$compiler=[CodeDom.Compiler.CompilerParameters]::new()
$compiler.GenerateExecutable=$true
$compiler.OutputAssembly=$helper
$compiler.CompilerOptions='/win32manifest:"' + (Join-Path $root 'app.manifest') + '"'
[void]$compiler.ReferencedAssemblies.AddRange([string[]]@('System.dll','System.Configuration.dll','System.Windows.Forms.dll','System.Drawing.dll'))
Add-Type -CompilerParameters $compiler -TypeDefinition @"
using System;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.Windows.Forms;
[assembly:System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2")]
public class RuntimeProbe {
 const BindingFlags AllStatic=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static;
 [STAThread] public static int Main(string[] args) {
  try {
   string executable=args[0], work=args[1], mode=args[2];
   var assembly=Assembly.LoadFrom(executable);
   if(mode=="prepare") {
    assembly.GetType("PingMonitor.RuntimeBootstrap").GetMethod("PrepareConfiguration",AllStatic).Invoke(null,new object[]{Path.Combine(work,"Runtime")});
    return 0;
   }
   if(mode=="old" || mode=="new") {
    var runtime=assembly.GetType("PingMonitor.RuntimeBootstrap");
    string config=(string)runtime.GetMethod("PrepareConfiguration",AllStatic).Invoke(null,new object[]{Path.Combine(work,"Runtime")});
    AppDomain domain;
    if(mode=="old") {
     File.Copy(config,executable+".config",true);
     domain=AppDomain.CreateDomain(Path.GetFileName(executable),null,new AppDomainSetup{ApplicationBase=work,ConfigurationFile=executable+".config",TargetFrameworkName=".NETFramework,Version=v4.7.2"});
    } else {
     File.Delete(executable+".config");
     domain=(AppDomain)runtime.GetMethod("CreateApplicationDomain",AllStatic).Invoke(null,new object[]{executable,config});
    }
    try { return domain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location,new[]{executable,work,mode=="old"?"save":"read"}); }
    finally { AppDomain.Unload(domain); }
   }
   // The helper has its own RuntimeProbe product identity and a unique executable path; real user preferences are never used.
   var pathsType=typeof(ConfigurationManager).Assembly.GetType("System.Configuration.ClientConfigPaths");
   object paths=pathsType.GetProperty("Current",AllStatic).GetValue(null,null);
   string naturalPath=(string)pathsType.GetProperty("LocalConfigFilename",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(paths,null);
   var settingsType=assembly.GetType("PingMonitor.Properties.Settings");
   var settings=(ApplicationSettingsBase)settingsType.GetProperty("Default").GetValue(null,null);
   if(mode=="save") {
    if((int)settings["UpdateInterval"]!=5000) throw new Exception("Default interval changed");
    settings["UpdateInterval"]=7000; settings["SelectedProviders"]="stc,mobily"; settings.Save();
   } else if((int)settings["UpdateInterval"]!=7000 || (string)settings["SelectedProviders"]!="stc,mobily") throw new Exception("Previous settings were lost");
   Application.EnableVisualStyles();
   var dpi=typeof(Form).Assembly.GetType("System.Windows.Forms.DpiHelper");
   bool improved=(bool)dpi.GetProperty("EnableDpiChangedMessageHandling",AllStatic).GetValue(null,null);
   string awareness=Convert.ToString(dpi.GetField("dpiAwarenessValue",AllStatic).GetValue(null));
   if(!improved || !String.Equals(awareness,"PerMonitorV2",StringComparison.OrdinalIgnoreCase)) throw new Exception("DPI initialization failed: "+awareness);
   if(mode=="read") {
    object store=Activator.CreateInstance(assembly.GetType("PingMonitor.MonitoringListStore"),new object[]{Path.Combine(work,"lists.xml")});
    using(Form form=(Form)Activator.CreateInstance(assembly.GetType("PingMonitor.Form1"),new[]{store})) {
     var f=BindingFlags.NonPublic|BindingFlags.Instance;
     form.GetType().GetField("updating",f).SetValue(form,false);
     ((Timer)form.GetType().GetField("pingTimer",f).GetValue(form)).Stop();
     form.StartPosition=FormStartPosition.Manual; form.Location=new System.Drawing.Point(-32000,-32000); form.ShowInTaskbar=false;
     form.Show(); Application.DoEvents();
     if(form.Controls.Find("cmbMonitorList",true).Length!=1) throw new Exception("Missing interface");
    }
   }
   File.WriteAllLines(Path.Combine(work,mode+".txt"),new[]{naturalPath,AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,awareness});
   if(mode=="read") { File.Delete(naturalPath); }
   return 0;
  } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
 }
}
"@
& $helper $exe $work old
if ($LASTEXITCODE -ne 0) {throw 'Legacy configuration test failed'}
& $helper $exe $work new
if ($LASTEXITCODE -ne 0) {throw 'Embedded configuration test failed'}
$old=Get-Content (Join-Path $work 'save.txt')
$current=Get-Content (Join-Path $work 'read.txt')
if ($old[0] -ne $current[0]) {throw 'Configuration relocation changed the user settings identity'}
if (Test-Path -LiteralPath ($exe+'.config')) {throw 'A sidecar was created next to EXE'}
# Validate missing, unchanged, damaged, locked and concurrently prepared runtime files.
$a=[Reflection.Assembly]::LoadFrom($exe)
$t=$a.GetType('PingMonitor.RuntimeBootstrap')
$prepare=$t.GetMethod('PrepareConfiguration',[Reflection.BindingFlags]'NonPublic,Static')
$runtime=Join-Path $work 'Runtime'
$config=[string]$prepare.Invoke($null,@([string]$runtime))
$before=[IO.File]::GetLastWriteTimeUtc($config)
[void]$prepare.Invoke($null,@([string]$runtime))
if ([IO.File]::GetLastWriteTimeUtc($config) -ne $before) {throw 'Unchanged config was rewritten'}
$expected=[IO.File]::ReadAllText($config)
[IO.File]::WriteAllText($config,'damaged')
$lock=[IO.File]::Open($config,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
try {
 $failed=$false; try {[void]$prepare.Invoke($null,@([string]$runtime))} catch {$failed=$true}
 if (-not $failed -or [IO.File]::ReadAllText($config) -ne 'damaged') {throw 'Locked replacement did not fail safely'}
} finally {$lock.Dispose()}
[void]$prepare.Invoke($null,@([string]$runtime))
if ([IO.File]::ReadAllText($config) -ne $expected) {throw 'Damaged configuration was not repaired'}
if (@(Get-ChildItem -LiteralPath $runtime -Filter '*.tmp').Count) {throw 'Temporary files leaked'}
Remove-Item -LiteralPath $config
$processes=@()
try {
 foreach ($attempt in 1..3) { $processes += Start-Process -FilePath $helper -ArgumentList ('"'+$exe+'" "'+$work+'" prepare') -WindowStyle Hidden -PassThru }
 foreach ($process in $processes) { if (-not $process.WaitForExit(15000)) {throw 'Concurrent startup timed out'}; if ($process.ExitCode -ne 0) {throw 'Concurrent configuration preparation failed'} }
} finally { foreach ($process in $processes) { if (-not $process.HasExited) {$process.Kill()}; $process.Dispose() } }
if ([IO.File]::ReadAllText($config) -ne $expected -or @(Get-ChildItem -LiteralPath $runtime -Filter '*.tmp').Count) {throw 'Concurrent startup damaged the cache'}
Write-Output 'PASS: EXE-only folder, embedded configuration, real WinForms DPI initialization, old/new settings identity, settings save/reload, isolated UI, stable cache, atomic repair, locked-file failure and concurrent startup.'
