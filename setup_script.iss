; Script được tạo bởi Antigravity AI cho dự án Auto Chinh Do
; Công cụ yêu cầu: Inno Setup (http://www.jrsoftware.org/isinfo.php)

#define MyAppName "Auto Chinh Do"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "LDPlayer Multi Tool"
#define MyAppExeName "auto_chinhdo.exe"
#define MyAppIcon "app_icon.ico"

[Setup]
; Thông tin định danh ứng dụng
AppId={{C789B1D2-A456-4E89-BABC-D123456789EF}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Cấu hình ứng dụng 64-bit
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
; Đường dẫn xuất file Setup sau khi Compile
OutputDir=.\InstallerOutput
OutputBaseFilename=AutoChinhDo_Setup_v{#MyAppVersion}
SetupIconFile={#MyAppIcon}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Yêu cầu quyền Admin để cài đặt (ghi file vào Program Files)
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Đóng gói toàn bộ thư mục ReadyToUse đã được kiểm chứng
Source: "bin\ReadyToUse\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppIcon}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppIcon}"; Tasks: desktopicon

[Run]
Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent shellexec
