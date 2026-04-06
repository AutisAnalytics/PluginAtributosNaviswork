@echo off
setlocal enabledelayedexpansion

:: =============================================================================
::  build_installer.bat
::  Autis Analytics - Navisworks Plugin
::  Pipeline: Compilar (Release) -> Preparar bundle -> Gerar EXE (InnoSetup)
::
::  Uso:
::    build_installer.bat
::    build_installer.bat /versao:1.2.0
:: =============================================================================

title Autis Analytics - Build do Instalador

set "PROJECT_DIR=%~dp0"
set "ISS_FILE=%PROJECT_DIR%installer\AutisAtributos.iss"
set "STAGING_DIR=%PROJECT_DIR%installer\staging"
set "OUTPUT_DIR=%PROJECT_DIR%installer\output"
set "APP_VERSION=1.0.0"

:: Processa argumentos
for %%A in (%*) do (
    set "ARG=%%A"
    if /i "!ARG:~0,8!"=="/versao:" set "APP_VERSION=!ARG:~8!"
)

echo.
echo  ============================================================
echo   Autis Analytics - Build do Instalador  v%APP_VERSION%
echo   Compilar -^> Bundle -^> EXE
echo  ============================================================
echo.

:: -------------------------------------------------------
:: 1. Verificar dotnet
:: -------------------------------------------------------
echo  [1/4] Verificando dotnet SDK...

where dotnet >nul 2>&1
if !errorlevel! neq 0 (
    echo  [ERRO] dotnet SDK nao encontrado.
    echo         Baixe em: https://dotnet.microsoft.com/download
    goto :fim_erro
)
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do echo         dotnet %%v

:: -------------------------------------------------------
:: 2. Compilar em Release x64
:: -------------------------------------------------------
echo.
echo  [2/4] Compilando Release x64...

if exist "%STAGING_DIR%\Contents\v23" rmdir /s /q "%STAGING_DIR%\Contents\v23"
mkdir "%STAGING_DIR%\Contents\v23" 2>nul

dotnet build "%PROJECT_DIR%AutisAnalytics.NavisworksAtributos.csproj" ^
    -c Release ^
    -p:PlatformTarget=x64 ^
    --nologo ^
    -v minimal

if !errorlevel! neq 0 (
    echo  [ERRO] Falha na compilacao.
    goto :fim_erro
)
if not exist "%STAGING_DIR%\Contents\v23\AutisAnalytics.NavisworksAtributos.dll" (
    echo  [ERRO] DLL nao encontrada apos compilacao.
    goto :fim_erro
)
echo         OK: AutisAnalytics.NavisworksAtributos.dll

:: -------------------------------------------------------
:: 3. Preparar staging (PackageContents.xml)
:: -------------------------------------------------------
echo.
echo  [3/4] Preparando bundle...

:: PackageContents.xml — fonte unica: staging do projeto
set "PKG_XML=%STAGING_DIR%\PackageContents.xml"
if not exist "%PKG_XML%" (
    echo  [ERRO] PackageContents.xml nao encontrado.
    goto :fim_erro
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$path = '%PKG_XML%';" ^
  "[xml]$xml = Get-Content -LiteralPath $path;" ^
  "$xml.ApplicationPackage.AppVersion = '%APP_VERSION%';" ^
  "$component = $xml.ApplicationPackage.Components.ComponentEntry;" ^
  "if ($null -eq $component) { throw 'ComponentEntry not found in PackageContents.xml.' };" ^
  "$component.Version = '%APP_VERSION%';" ^
  "$xml.Save($path)"

if !errorlevel! neq 0 (
    echo  [ERRO] Falha ao atualizar versao no PackageContents.xml.
    goto :fim_erro
)

echo         PackageContents.xml atualizado para v%APP_VERSION%.
echo         Staging pronto: %STAGING_DIR%

:: -------------------------------------------------------
:: 4. Gerar instalador EXE (InnoSetup)
:: -------------------------------------------------------
echo.
echo  [4/4] Gerando instalador EXE (InnoSetup)...

set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe"      set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC (
    where iscc >nul 2>&1
    if !errorlevel!==0 set "ISCC=iscc"
)
if not defined ISCC (
    echo  [ERRO] InnoSetup 6 nao encontrado.
    echo         Baixe em: https://jrsoftware.org/isdl.php
    goto :fim_erro
)
echo         OK: %ISCC%

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

"%ISCC%" "/DAppVersion=%APP_VERSION%" "%ISS_FILE%"

if !errorlevel! neq 0 (
    echo  [ERRO] Falha ao gerar o instalador.
    goto :fim_erro
)

:: -------------------------------------------------------
:: Sucesso
:: -------------------------------------------------------
echo.
echo  ============================================================
echo   BUILD CONCLUIDO com sucesso!
echo  ============================================================
echo.
for %%F in ("%OUTPUT_DIR%\*.exe") do (
    echo   Arquivo: %%~nxF
    echo   Tamanho: %%~zF bytes
    echo   Local:   %OUTPUT_DIR%
)
echo.
echo   Installation instructions:
echo     1. Copy the .exe to the target PC
echo     2. Run as a normal user (no Administrator required)
echo     3. Open Navisworks 2026 -- the "AWP Autis" tab appears automatically
echo.
goto :fim_ok

:fim_erro
echo.
pause
exit /b 1

:fim_ok
pause
exit /b 0
