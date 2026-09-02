@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.3.4 - SURVIVAL + PROGRESSION
echo =========================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  echo Cai .NET SDK 6 hoac SDK moi hon roi chay lai file nay.
  echo.
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha34.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo.
explorer "%~dp0release"
pause
