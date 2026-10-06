[Setup]
AppName=Battery Service Studio
AppVersion=1.1.0
DefaultDirName={pf}\Battery Service Studio
DefaultGroupName=Battery Service Studio
OutputDir=installer
OutputBaseFilename=BatteryServiceStudio_Setup_Win7
Compression=lzma
SolidCompression=yes
UninstallDisplayIcon={app}\BatteryServiceStudio.exe
PrivilegesRequired=admin

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Battery Service Studio"; Filename: "{app}\BatteryServiceStudio.exe"
Name: "{userdesktop}\Battery Service Studio"; Filename: "{app}\BatteryServiceStudio.exe"

[Run]
Filename: "{app}\BatteryServiceStudio.exe"; Description: "تشغيل البرنامج"; Flags: nowait postinstall skipifsilent