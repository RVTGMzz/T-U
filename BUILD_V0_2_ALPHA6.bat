@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.5 - SWITCH + CODEX PROFILE HOTFIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha665.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.5.
echo - Switch Action Button: SMAPI authoritative input.
echo - Codex vertical navigation: 1 profile/input.
echo - Profile right panel: uniform scale 1.52.
echo - Unsupported UI punctuation replaced to remove hollow-star glyph fallback.
explorer "%~dp0release"
pause