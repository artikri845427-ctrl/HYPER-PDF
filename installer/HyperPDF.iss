; Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
;
; Inno Setup script of HyperPDF.
;
; The branding values are not hard coded here, they are generated into brand.iss by
; installer\build-installer.ps1 from Directory.Build.props.

#include "brand.iss"

#ifndef BrandFileIncluded
  #error "brand.iss is missing. Run installer\build-installer.ps1 instead of compiling this script directly."
#endif

#define MyAppName Brand_ProductName
#define MyAppVersion Brand_Version
#define MyAppPublisher Brand_Publisher
#define MyAppCopyright Brand_Copyright
#define MyAppDescription Brand_Description
#define MyAppContact Brand_Contact
#define MyAppId Brand_AppId

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#Brand_RepositoryUrl}
AppSupportURL={#Brand_RepositoryUrl}
AppUpdatesURL={#Brand_RepositoryUrl}
AppCopyright={#MyAppCopyright}
AppComments={#MyAppDescription}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
UninstallDisplayName={#MyAppName} {#MyAppVersion}
UninstallDisplayIcon={app}\HyperPDF.exe
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}
OutputDir=..\build\installer
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\icon\HyperPDF.ico
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "associate"; Description: "Add HyperPDF to the &Open with list of PDF documents"; GroupDescription: "File associations:"

[Files]
; The application itself.
Source: "..\build\app\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion
; The native Shell extensions (x64 and x86) including their PDFium build.
Source: "..\build\native\x64\HyperPDFThumbnailProvider.dll"; DestDir: "{app}\Shell\x64"; Flags: ignoreversion
Source: "..\build\native\x64\HyperPDFPreviewHandler.dll"; DestDir: "{app}\Shell\x64"; Flags: ignoreversion
Source: "..\build\native\x64\pdfium.dll"; DestDir: "{app}\Shell\x64"; Flags: ignoreversion
Source: "..\build\native\x86\HyperPDFThumbnailProvider.dll"; DestDir: "{app}\Shell\x86"; Flags: ignoreversion
Source: "..\build\native\x86\HyperPDFPreviewHandler.dll"; DestDir: "{app}\Shell\x86"; Flags: ignoreversion
Source: "..\build\native\x86\pdfium.dll"; DestDir: "{app}\Shell\x86"; Flags: ignoreversion
; The readme of the start menu entry.
Source: "..\build\app\README.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\HyperPDF.exe"; WorkingDir: "{app}"; IconFilename: "{app}\HyperPDF.exe"
Name: "{group}\{#MyAppName} (readme)"; Filename: "{app}\README.txt"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\HyperPDF.exe"; WorkingDir: "{app}"; IconFilename: "{app}\HyperPDF.exe"; Tasks: desktopicon

[Registry]
; The application registers itself for the default programs dialog.
Root: HKLM; Subkey: "Software\Classes\Applications\HyperPDF.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\Applications\HyperPDF.exe"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "{#MyAppDescription}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\Applications\HyperPDF.exe\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\HyperPDF.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\Applications\HyperPDF.exe\SupportedTypes"; ValueType: string; ValueName: ".pdf"; ValueData: ""; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: "Software\Classes\Applications\HyperPDF.exe\shell\open\command"; Flags: uninsdeletekey

; The PDF capabilities describe the supported file type to Windows.
Root: HKLM; Subkey: "Software\{#MyAppName}\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "{#MyAppDescription}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pdf"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey

[Run]
; The native Shell extensions register themselves in the registry.
Filename: "{sys}\regsvr32.exe"; Parameters: "/s ""{app}\Shell\x64\HyperPDFThumbnailProvider.dll"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering the HyperPDF thumbnail provider..."
Filename: "{sys}\regsvr32.exe"; Parameters: "/s ""{app}\Shell\x64\HyperPDFPreviewHandler.dll"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering the HyperPDF preview handler..."
Filename: "{syswow64}\regsvr32.exe"; Parameters: "/s ""{app}\Shell\x86\HyperPDFThumbnailProvider.dll"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering the HyperPDF thumbnail provider (32 bit)..."
Filename: "{syswow64}\regsvr32.exe"; Parameters: "/s ""{app}\Shell\x86\HyperPDFPreviewHandler.dll"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering the HyperPDF preview handler (32 bit)..."
Filename: "{app}\HyperPDF.exe"; Description: "Start {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\regsvr32.exe"; Parameters: "/s /u ""{app}\Shell\x64\HyperPDFThumbnailProvider.dll"""; Flags: runhidden waituntilterminated; RunOnceId: "UnregisterThumbnail64"
Filename: "{sys}\regsvr32.exe"; Parameters: "/s /u ""{app}\Shell\x64\HyperPDFPreviewHandler.dll"""; Flags: runhidden waituntilterminated; RunOnceId: "UnregisterPreview64"
Filename: "{syswow64}\regsvr32.exe"; Parameters: "/s /u ""{app}\Shell\x86\HyperPDFThumbnailProvider.dll"""; Flags: runhidden waituntilterminated; RunOnceId: "UnregisterThumbnail32"
Filename: "{syswow64}\regsvr32.exe"; Parameters: "/s /u ""{app}\Shell\x86\HyperPDFPreviewHandler.dll"""; Flags: runhidden waituntilterminated; RunOnceId: "UnregisterPreview32"

[Code]
const
  DotNetRuntimeUrl = 'https://dotnet.microsoft.com/download/dotnet/8.0';
  OpenWithKey = 'Software\Classes\.pdf\shell\HyperPDF';

/// Returns true when the .NET 8 Desktop Runtime is installed.
function IsDotNetDesktopRuntimeInstalled(): Boolean;
var
  FindRec: TFindRec;
  Path: string;
begin
  Result := False;
  Path := ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App');

  if not DirExists(Path) then
    Exit;

  if FindFirst(Path + '\*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          if (FindRec.Name <> '.') and (FindRec.Name <> '..') and (Copy(FindRec.Name, 1, 2) = '8.') then
          begin
            Result := True;
            Exit;
          end;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

/// Offers the download of the .NET runtime when it is missing.
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  if not IsDotNetDesktopRuntimeInstalled() then
  begin
    if MsgBox('HyperPDF needs the .NET 8 Desktop Runtime.' + #13#10 + #13#10 +
      'The runtime is not installed on this computer.' + #13#10 + #13#10 +
      'Download it now and continue with the installation?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', DotNetRuntimeUrl, '', '', SW_SHOW, ewNoWait, ResultCode);
      Result := False;
      MsgBox('The download has been started. Please run the setup again once the runtime is installed.',
        mbInformation, MB_OK);
    end;
  end;
end;

/// Adds the "Open with HyperPDF" entry when the task is selected and shows the credit line.
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    WizardForm.FinishedLabel.Caption := WizardForm.FinishedLabel.Caption + #13#10 + #13#10 +
      '{#Brand_CreditLine}';

    if WizardIsTaskSelected('associate') then
    begin
      RegWriteStringValue(HKLM, OpenWithKey, '', '');
      RegWriteStringValue(HKLM, OpenWithKey, 'MUIVerb', 'Open with {#Brand_ProductName}');
      RegWriteStringValue(HKLM, OpenWithKey + '\command', '',
        '"' + ExpandConstant('{app}\HyperPDF.exe') + '" "%1"');
    end;
  end;
end;

/// Removes the "Open with HyperPDF" entry during the uninstall.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RegDeleteValue(HKLM, OpenWithKey + '\command', '');
    RegDeleteValue(HKLM, OpenWithKey, 'MUIVerb');
    RegDeleteValue(HKLM, OpenWithKey, '');
  end;
end;
