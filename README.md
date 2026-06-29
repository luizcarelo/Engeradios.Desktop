Engeradios C3 - Centro de Comando 📻

Sistema de Missão Crítica para Gravação, Gestão e Auditoria de Rádio-Comunicações.

O Engeradios C3 é uma aplicação Desktop avançada construída em .NET 10 (WPF). Foi desenhada para operar em ambientes de alta disponibilidade (24/7), garantindo a captação de áudio em tempo real com Deteção de Atividade de Voz (VAD), armazenamento inteligente e sincronização segura com a nuvem (Cloud Sync).

✨ Funcionalidades Principais

🎙️ Motor de Áudio de Alta Performance

Gravação Multicanal: Suporte para múltiplas placas de som em simultâneo utilizando a biblioteca NAudio.

VAD (Voice Activity Detection): Deteção inteligente de voz com sensibilidade configurável por canal, ignorando ruídos de fundo e poupando espaço em disco.

Compressão Nativa: Conversão automática de ficheiros .wav para .mp3 em tempo real através da API do Windows Media Foundation.

💾 Gestão Inteligente de Armazenamento (FIFO)

Definição de limites de disco dinâmicos (20GB, 40GB, 60GB, 100GB).

Rotina automática de eliminação de ficheiros mais antigos (FIFO) quando o limite é atingido.

Sistema de "Cadeado" (Proteção) que impede a eliminação de áudios críticos ou marcados como prova.

☁️ Resiliência e Cloud Sync

Sincronização em segundo plano com a API Web (KingHost).

Exponential Backoff Retry: Política de tentativas automáticas à prova de falhas de rede (micro-cortes).

Telemetria e Health Check constantes para monitorização remota de máquinas ativas.

🔒 Segurança e Rastreabilidade (Auditoria)

Execução Blindada: Requer privilégios de Administrador do Windows nativamente (app.manifest).

Autenticação Local: Palavras-passe encriptadas (Hash SHA-256) em base de dados SQLite.

Log de Auditoria: Rastreabilidade rigorosa de todas as ações (quem ouviu, quem exportou, quem protegeu um áudio e encerramentos do sistema).

🛠️ Arquitetura e Tecnologias

O projeto segue estritamente os princípios do Clean Code e a arquitetura MVVM (Model-View-ViewModel).

Framework: .NET 10.0 (Windows Presentation Foundation - WPF)

Padrão de Desenho: MVVM via CommunityToolkit.Mvvm

Injeção de Dependências (DI): Microsoft.Extensions.DependencyInjection

Base de Dados: SQLite (Microsoft.Data.Sqlite)

Processamento de Áudio: NAudio

Interface (UI/UX): Controlos WPF nativos estilizados (Flat/Material Design), com suporte fluido a Tema Claro e Escuro.

🚀 Como Compilar e Executar

Pré-requisitos

Visual Studio 2022 (versão mais recente com suporte a .NET 10)

.NET 10 Desktop Runtime

Placa(s) de som ativa(s) ou microfones ligados ao sistema para testes.

Passos para Instalação

Clone o repositório:

git clone [https://github.com/seu-usuario/engeradios-desktop.git](https://github.com/seu-usuario/engeradios-desktop.git)


Abra a solução Engeradios.Desktop.slnx no Visual Studio.

Restaure os pacotes NuGet (se não for automático):

dotnet restore


Configure o projeto Engeradios.Desktop como projeto de arranque (Startup Project).

Compile e execute (F5).

Nota de Segurança: O Visual Studio poderá reiniciar com privilégios de Administrador para conseguir correr e depurar a aplicação, devido às exigências do app.manifest.

📁 Estrutura do Projeto

Engeradios.Desktop/
├── Helpers/          # Conversores UI (IValueConverter) e classes de Segurança (Hashing)
├── Models/           # Modelos de Dados (RegistoAudio, Usuario, LogAuditoria, etc.)
├── Services/         # Lógica de Negócio (Áudio, Cloud, Base de Dados, Configurações)
├── ViewModels/       # Lógica de Apresentação (MVVM)
├── InstaladorFinal/  # Scripts do Inno Setup (.iss) e binários compilados
├── App.xaml          # Ponto de entrada e Injeção de Dependências
└── ...               # Views em XAML (MainWindow, LoginWindow, ConfiguracoesWindow, etc.)


👨‍💻 Fluxo de Primeiro Uso (Onboarding)

Ao iniciar a aplicação pela primeira vez num computador novo (sem base de dados prévia):

O sistema detetará o estado inicial e forçará o ecrã de Setup.

Será exigida a criação de uma palavra-passe forte para o Administrador Principal.

Apenas o Administrador poderá adicionar Canais de Áudio, atribuir placas de hardware e definir limites de armazenamento na janela de Configurações.

📄 Licença e Uso

Este software é proprietário e foi desenvolvido para as operações do Centro de Comando Engeradios.
A cópia, distribuição ou modificação não autorizada deste código fonte é estritamente proibida.

© 2026 Engeradios - Todos os direitos reservados.
