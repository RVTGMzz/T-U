@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.1.3 - PARTY UX + TANK FIX
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.1.3.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo Sau khi vao game, SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.1.3
echo Test: ChaCha khong co Thu nap, Alex di toi quái truoc khi TAUNT, Trang bi mo panel rieng.
echo.
explorer "%~dp0release"
pause
