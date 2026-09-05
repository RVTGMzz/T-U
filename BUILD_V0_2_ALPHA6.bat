@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.6 - SWITCH UNEQUIP HOTFIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha666.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.6.
echo - Switch Action Button: semantic equip/activate.
echo - Switch Use Tool Button: semantic unequip.
echo - Transactional equipment safety from 6.6.4/6.6.5 preserved.
echo - Codex/profile/font fixes from 6.6.5 preserved.
explorer "%~dp0release"
pause