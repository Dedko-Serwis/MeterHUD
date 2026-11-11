#define MyAppName "MeterHUD"
#ifndef MyAppVersion
  #define MyAppVersion "0.2.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\dist\publish"
#endif

[Setup]
AppId={{D35E2F19-9C84-4E53-8F6D-3B65D2F30EAB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=DEDKO Serwis
AppPublisherURL=https://github.com/Dedko-Serwis/MeterHUD
AppSupportURL=https://github.com/Dedko-Serwis/MeterHUD/issues
AppUpdatesURL=https://github.com/Dedko-Serwis/MeterHUD/releases/latest
DefaultDirName={localappdata}\Programs\MeterHUD
DefaultGroupName=MeterHUD
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=MeterHUD-{#MyAppVersion}-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=no
UninstallDisplayIcon={app}\MeterHUD.exe
CloseApplications=yes
RestartApplications=no
ArchitecturesAllowed=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[CustomMessages]
english.DesktopShortcut=Create a desktop shortcut
polish.DesktopShortcut=Utwórz skrót na pulpicie
english.LaunchApp=Launch MeterHUD
polish.LaunchApp=Uruchom MeterHUD

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\MeterHUD"; Filename: "{app}\MeterHUD.exe"
Name: "{autodesktop}\MeterHUD"; Filename: "{app}\MeterHUD.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:DesktopShortcut}"; Flags: unchecked

[Run]
Filename: "{app}\MeterHUD.exe"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent
