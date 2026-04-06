; =============================================================================
;  Autis Analytics - Navisworks Plugin Installer
;  InnoSetup 6.x
;
;  Como compilar:
;    iscc AutisAtributos.iss
;    iscc /DAppVersion=1.2.0 AutisAtributos.iss
; =============================================================================

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName      "Autis Analytics - Navisworks Plugin"
#define AppPublisher "Autis Analytics"
#define BundleName   "AutisAtributos.bundle"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} v{#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://autisanalytics.com.br
AppSupportURL=https://autisanalytics.com.br/suporte

; Instala em AppData do usuario — nao requer Administrador
DefaultDirName={userappdata}\Autodesk\ApplicationPlugins\{#BundleName}
DisableDirPage=yes
DisableProgramGroupPage=yes
CreateUninstallRegKey=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={userappdata}\Autodesk\ApplicationPlugins\{#BundleName}\Contents\v23\AutisAnalytics.NavisworksAtributos.dll

; Sem senha — distribuicao livre
Encryption=no

; Saida
OutputDir=output
OutputBaseFilename=AutisAtributos_v{#AppVersion}_Setup

; Compressao maxima (LZMA2)
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Metadados do EXE
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} - Instalador
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

; Requisitos
MinVersion=6.1sp1
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

; Visual
WizardStyle=modern
ShowLanguageDialog=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to the installer for%n{#AppName}
WelcomeLabel2=This wizard will install the Autis Analytics plugin for Autodesk Navisworks 2026.%n%nClose Navisworks before continuing.%n%nClick Next to continue.
FinishedHeadingLabel=Installation complete
FinishedLabel=The plugin was installed successfully.%n%nOpen Navisworks 2026 — the "AWP Autis" tab will appear automatically in the ribbon.

[Dirs]
Name: "{app}\Contents\v23"; Flags: uninsalwaysuninstall
Name: "{app}";              Flags: uninsalwaysuninstall

[Files]
; Manifesto do bundle (obrigatorio para o Navisworks reconhecer o plugin)
Source: "staging\PackageContents.xml"; DestDir: "{app}"; Flags: ignoreversion

; DLL principal do plugin
Source: "staging\Contents\v23\AutisAnalytics.NavisworksAtributos.dll"; DestDir: "{app}\Contents\v23"; Flags: ignoreversion

[Registry]
Root: HKCU; Subkey: "Software\AutisAnalytics\NavisworksPlugin"; ValueType: string; ValueName: "Version";     ValueData: "{#AppVersion}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\AutisAnalytics\NavisworksPlugin"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKCU; Subkey: "Software\AutisAnalytics\NavisworksPlugin"; ValueType: string; ValueName: "InstallDate"; ValueData: "{code:GetInstallDate}"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Contents\v23"

[Code]
function GetInstallDate(Param: String): String;
begin
  Result := GetDateTimeString('yyyy/mm/dd', #0, #0);
end;

function NavisworksInstalled(): Boolean;
begin
  Result := DirExists('C:\Program Files\Autodesk\Navisworks Manage 2026') or
            DirExists('C:\Program Files\Autodesk\Navisworks Simulate 2026');
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not NavisworksInstalled() then
  begin
    if MsgBox('Autodesk Navisworks 2026 was not found on this computer.' + #13#10 +
              'The plugin requires Navisworks Manage or Simulate 2026 (64-bit).' + #13#10 + #13#10 +
              'Do you want to continue anyway?',
              mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if CheckForMutexes('Local\AutodeskNavisworks') then
  begin
    MsgBox('Autodesk Navisworks is open.' + #13#10 +
           'Close Navisworks before continuing with the installation.',
           mbError, MB_OK);
    Result := 'Close Navisworks and run the installer again.';
  end;
end;
