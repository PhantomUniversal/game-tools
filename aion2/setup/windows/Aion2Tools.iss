; The installer for people who use AION2 Tools but do not build it. build-installer.bat publishes
; into src\Aion2Tools\bin\installer\win-x64 first and passes the version and output folder.
; Per user under %LocalAppData%\Programs: the app writes its settings beside its executable.

#ifndef AppVersion
  #define AppVersion "0"
#endif

[Setup]
AppId={{B2E4A1C7-5D3F-4E8A-9B61-7C0D3F2A8E14}
AppName=AION2 Tools
AppVersion={#AppVersion}
AppPublisher=Phantom
DefaultDirName={userpf}\Aion2Tools
DefaultGroupName=AION2 Tools
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\dist
OutputBaseFilename=Aion2ToolsSetup
UninstallDisplayIcon={app}\Aion2Tools.exe
UninstallFilesDir={app}\Uninstall
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\..\src\Aion2Tools\bin\installer\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\AION2 Tools"; Filename: "{app}\Aion2Tools.exe"
Name: "{group}\Uninstall AION2 Tools"; Filename: "{uninstallexe}"
Name: "{userdesktop}\AION2 Tools"; Filename: "{app}\Aion2Tools.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Aion2Tools.exe"; Description: "{cm:LaunchProgram,AION2 Tools}"; Flags: nowait postinstall skipifsilent
