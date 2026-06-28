; ======================================================================
; SCRIPT DE INSTALAÇÃO - ENGERADIOS Gravador
; ======================================================================

[Setup]
; Identificação do Programa
AppName=Engeradios Gravador Command Center
AppVersion=2.1.0
AppPublisher=Engeradios Segurança Eletrônica
AppPublisherURL=https://www.engeradios.com.br
AppSupportURL=https://www.engeradios.com.br/suporte

; Pasta de Instalação Padrão (Program Files)
DefaultDirName={autopf}\Engeradios Gravador
DefaultGroupName=Engeradios

; Nome e localização do ficheiro de Setup gerado
OutputDir=.\InstaladorFinal
OutputBaseFilename=Setup_EngeradiosGravador_v2.1.0

; Estética do Instalador
SetupIconFile=.\icon.ico
UninstallDisplayIcon={app}\Engeradios.Desktop.exe
Compression=lzma2/ultra
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

; Privilégios de Administrador obrigatórios (essencial para gravação de áudio)
PrivilegesRequired=admin

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; ATENÇÃO: Verifique se o caminho "Source" aponta para a pasta Release correta do seu projeto!
Source: ".\bin\Release\net10.0-windows\Engeradios.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\net10.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Adiciona o ícone e imagens necessárias
Source: ".\icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\logo_claro.png"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\logo_escuro.png"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Cria os atalhos com o ícone oficial
Name: "{group}\Engeradios Gravador"; Filename: "{app}\Engeradios.Desktop.exe"; IconFilename: "{app}\icon.ico"
Name: "{group}\Desinstalar Engeradios Gravador"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Engeradios Gravador"; Filename: "{app}\Engeradios.Desktop.exe"; Tasks: desktopicon; IconFilename: "{app}\icon.ico"

[Run]
; Inicia automaticamente após instalar
Filename: "{app}\Engeradios.Desktop.exe"; Description: "{cm:LaunchProgram,Engeradios Gravador}"; Flags: nowait postinstall skipifsilent

[Dirs]
; Garante que a pasta oculta de áudios no C:\ProgramData\Engeradios_Audios exista e tenha permissões para o Windows não bloquear as gravações
Name: "{commonappdata}\Engeradios_Audios"; Permissions: users-modify