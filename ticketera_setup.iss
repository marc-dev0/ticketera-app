; ─────────────────────────────────────────────────────────────────────────────
; Inno Setup Script – ProfitCode / Ticketera EAN-13
; Desarrollado por Miguel Rojas © 2026
; ─────────────────────────────────────────────────────────────────────────────

#define AppName      "ProfitCode"
#define AppVersion   "1.4.3"
#define AppPublisher "Miguel Rojas – Profitzen"
#define AppExeName   "TicketeraApp.exe"
#define SourceDir    "bin\installer_source"

[Setup]
AppId={{7A3F1B2C-9E4D-4A8B-BC6F-12D3E5F7A901}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisherURL=https://github.com/marc-dev0/ticketera-app
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=bin\installer_output
OutputBaseFilename=ProfitCode_Setup_v{#AppVersion}

Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear icono en el Escritorio"; GroupDescription: "Tareas adicionales:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Iniciar {#AppName} ahora"; Flags: nowait postinstall skipifsilent
