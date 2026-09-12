PingMonitor.SA.v1 1.3.1

Requires Windows 10 or later and .NET Framework 4.7.2.
Run PingMonitor.SA.v1.exe. No external .exe.config is required.
Runtime configuration is embedded and prepared automatically in:
%LOCALAPPDATA%\PingMonitor.SA\Runtime
Keep .NET Framework installed; this is a single distribution EXE, not a bundled .NET runtime.
When upgrading, close the old app and replace EXE in the same folder.
The old .exe.config is no longer needed and may be removed manually.

Monitoring lists and display preferences are stored in:
%LOCALAPPDATA%\PingMonitor.SA\server-lists.v1.xml
AppData is hidden by default. Personal lists are not stored beside the EXE.
Measurement history stays in memory; use CSV export to keep it.

Source and published releases:
https://github.com/abooodHub/PingMonitor_SA
