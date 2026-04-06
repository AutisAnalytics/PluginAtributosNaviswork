===============================================================
  AUTIS ANALYTICS - NAVISWORKS PLUGIN
  Guia de Build e Distribuição do Instalador
===============================================================

PRE-REQUISITOS (maquina de desenvolvimento):
  - Visual Studio 2022 (com .NET Framework 4.8)
  - Autodesk Navisworks Manage 2026 (para as DLLs de referencia)
  - InnoSetup 6.x: https://jrsoftware.org/isdl.php

---------------------------------------------------------------
COMO GERAR O INSTALADOR
---------------------------------------------------------------

OPCAO 1 - Build padrao:
  Execute:
    build_installer.bat

OPCAO 2 - Definindo a versao do instalador:
  Execute:
    build_installer.bat /versao:1.2.0

OPCAO 3 - Compilar manualmente via InnoSetup:
  1. Compile o projeto no Visual Studio em Release x64
  2. Garanta que o PackageContents.xml em installer\staging\ esteja correto
  3. Execute:
       "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DAppVersion=1.2.0 installer\AutisAtributos.iss

O instalador gerado estara em:
  installer\output\AutisAtributos_v<versao>_Setup.exe

---------------------------------------------------------------
COMO INSTALAR EM OUTRO PC
---------------------------------------------------------------

Requisitos do PC de destino:
  - Windows 10/11 64-bit
  - Autodesk Navisworks Manage ou Simulate 2026
  - NAO precisa de permissao de Administrador

Passos:
  1. Copie o arquivo AutisAtributos_v<versao>_Setup.exe
  2. Execute como usuario normal
  3. Siga o assistente de instalacao
  4. Abra o Navisworks 2026
  5. A aba "AWP Autis" aparecera automaticamente na ribbon

---------------------------------------------------------------
DESINSTALAR
---------------------------------------------------------------

Via Painel de Controle > Programas > Autis Analytics - Navisworks Plugin
Ou execute o desinstalador em %AppData%\Autodesk\ApplicationPlugins\AutisAtributos.bundle\

---------------------------------------------------------------
ESTRUTURA DOS ARQUIVOS
---------------------------------------------------------------

  plugin_navisworks\
  ├── AutisAnalytics.NavisworksAtributos.csproj
  ├── build_installer.bat          <- Script de build completo
  ├── installer\
  │   ├── AutisAtributos.iss       <- Script InnoSetup do instalador
  │   ├── staging\                 <- Arquivos empacotados usados no build
  │   │   ├── PackageContents.xml
  │   │   └── Contents\v23\
  │   │       └── AutisAnalytics.NavisworksAtributos.dll
  │   └── output\                  <- EXE instalador gerado aqui
  │       └── AutisAtributos_v<versao>_Setup.exe

===============================================================
