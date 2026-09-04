@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.5.1 - MIMI RECRUIT GATE HARDENING
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha651.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.5.1.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.5.1
echo.
echo TEST NHANH:
echo - MiMi con ??? hoac Cardcha chua tao friendship unlock: KHONG recruit.
echo - Event/dialogue/menu Cardcha dang mo: KHONG recruit.
echo - Sau Wizard meetup, MiMi social visible va co the tuong tac: CO THE recruit.
echo - Team Up khong hardcode lich merchant/vi tri social sau unlock.
echo - ChaCha van Special Companion, khong Main Party.
echo - Regression Origin + The Surge + Sudoku + equipment + Party Vault.
echo.
explorer "%~dp0release"
pause
