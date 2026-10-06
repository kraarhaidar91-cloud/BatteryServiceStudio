[Setup]
AppName=Battery Service Studio
AppVersion=1.0.0
DefaultDirName={autopf}\Battery Service Studio
DefaultGroupName=Battery Service Studio
OutputDir=installer
OutputBaseFilename=BatteryServiceStudio_Setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\BatteryServiceStudio.exe

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Battery Service Studio"; Filename: "{app}\BatteryServiceStudio.exe"
Name: "{autodesktop}\Battery Service Studio"; Filename: "{app}\BatteryServiceStudio.exe"

[Run]
Filename: "{app}\BatteryServiceStudio.exe"; Description: "تشغيل البرنامج"; Flags: nowait postinstall skipifsilent