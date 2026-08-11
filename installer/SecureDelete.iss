; Inno Setup script for SecureDelete — a per-user, no-elevation install that wraps
; Microsoft Sysinternals SDelete and registers a classic Explorer context-menu verb.
;
; Build:  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\SecureDelete.iss
; (Publish the app first: dotnet publish ... -o artifacts\publish)

#define AppName "SecureDelete"
; Version can be injected by CI with ISCC /DAppVersion=x.y.z; otherwise this default is used.
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define AppPublisher "Jessie Wadman"
#define AppExe "SecureDelete.exe"
#define AppUrl "https://learn.microsoft.com/en-us/sysinternals/downloads/sdelete"

[Setup]
AppId={{9F3B7C1E-4A2D-4E88-9C3A-7B1E5D2F6A10}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppComments=Safe Explorer wrapper around Microsoft Sysinternals SDelete. SDelete is a separate Microsoft download and is not bundled.
VersionInfoVersion={#AppVersion}

; Per-user install: no elevation required, enables clean HKCU context-menu registration.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

DefaultDirName={localappdata}\Programs\{#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}

; Make the SDelete prerequisite clear before anything is installed.
InfoBeforeFile=SDELETE-NOTE.txt

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes

OutputDir=..\artifacts\installer
OutputBaseFilename={#AppName}-{#AppVersion}-Setup

[Files]
; Self-contained publish output (application + .NET runtime + WPF). SDelete is NOT included.
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Registry]
; ---- Files: HKCU\Software\Classes\*\shell\SecureDelete ----
Root: HKCU; Subkey: "Software\Classes\*\shell\SecureDelete"; ValueType: string; ValueName: ""; ValueData: "Secure Delete"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\*\shell\SecureDelete"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#AppExe},0"
; CommandFlags = ECF_SEPARATORBEFORE (0x20) | ECF_SEPARATORAFTER (0x40) => separators above and below.
Root: HKCU; Subkey: "Software\Classes\*\shell\SecureDelete"; ValueType: dword; ValueName: "CommandFlags"; ValueData: "$00000060"
Root: HKCU; Subkey: "Software\Classes\*\shell\SecureDelete\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" --delete ""%1"""

; ---- Directories: HKCU\Software\Classes\Directory\shell\SecureDelete ----
Root: HKCU; Subkey: "Software\Classes\Directory\shell\SecureDelete"; ValueType: string; ValueName: ""; ValueData: "Secure Delete"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\SecureDelete"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#AppExe},0"
Root: HKCU; Subkey: "Software\Classes\Directory\shell\SecureDelete"; ValueType: dword; ValueName: "CommandFlags"; ValueData: "$00000060"
Root: HKCU; Subkey: "Software\Classes\Directory\shell\SecureDelete\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" --delete ""%1"""

[Run]
; Refresh Explorer's shell associations so the new verb appears without a sign-out.
Filename: "{sys}\ie4uinit.exe"; Parameters: "-show"; Flags: runhidden nowait skipifdoesntexist

; No [Icons] (no Start-menu/desktop shortcuts), no service, no startup entry, no telemetry.
