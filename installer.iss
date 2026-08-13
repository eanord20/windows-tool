[Setup]
AppName=NS PDF Merge
AppVersion=1.2.0
DefaultDirName={autopf}\NSPdfMerge
DefaultGroupName=NS PDF Merge
OutputDir=installer_output
OutputBaseFilename=NSPdfMerge_Setup
Compression=lzma
SolidCompression=yes
SetupIconFile=flux-dev_A_futuristic_icon_for_a_program_that_combines_PDF_files_into_one_the_program_is_-1.ico
UninstallDisplayIcon={app}\NSPdfMerge.exe
WizardStyle=modern
PrivilegesRequired=admin

[Files]
Source: "src\NSPdfMerge.App\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\NS PDF Merge"; Filename: "{app}\NSPdfMerge.exe"; IconFilename: "{app}\NSPdfMerge.exe"
Name: "{group}\Uninstall NS PDF Merge"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\NSPdfMerge.exe"; Description: "Launch NS PDF Merge"; Flags: nowait postinstall skipifsilent
