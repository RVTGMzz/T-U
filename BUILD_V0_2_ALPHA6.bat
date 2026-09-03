@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.4.3 - CARDCHA COMBAT SANDBOX
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha643.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.4.3.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.4.3
echo Test nhanh: teamup_test sandbox normal
echo Arena phai la Cardcha_CardTestArena, KHONG phai map Region I / map tau.
echo Waves: easy cap 5, normal cap 8, hard cap 11.
echo teamup_test spawn boss = 1 boss test HP cao.
echo teamup_test arena exit = dung Cardcha Lab lifecycle va quay ve.
echo.
explorer "%~dp0release"
pause
