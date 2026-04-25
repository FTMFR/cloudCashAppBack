@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

set "SHOULD_PAUSE="
if "%~1"=="" set "SHOULD_PAUSE=1"

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%\setup-production.ps1" -PublishPath "%SCRIPT_DIR%" %*
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
  echo [ERROR] Production setup failed.
  if defined SHOULD_PAUSE pause
  exit /b %EXIT_CODE%
)

echo [OK] Production setup completed.
if defined SHOULD_PAUSE pause
exit /b 0
