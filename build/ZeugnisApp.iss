; Inno Setup script for the Zeugnis-App installer (plan §Sprint 12).
; Build the payload first:  pwsh build/publish.ps1
; Then compile this script with Inno Setup 6+ (iscc build/ZeugnisApp.iss).
; Finally Authenticode-sign the produced Setup .exe (see docs/release-checklist.md).

#define AppName "Zeugnis-App"
#define AppPublisher "Grundschule Wehringhausen"
#define AppVersion "0.1.0"
#define AppExeName "ZeugnisApp.exe"
#define PayloadDir "..\publish\win-x64"

[Setup]
AppId={{7E3B2A10-6C4D-4F2E-9A1B-2C7D8E5F0A31}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputBaseFilename=ZeugnisApp-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Note: user data lives in %LOCALAPPDATA%\Zeugnis-App and is deliberately NOT removed on uninstall.
