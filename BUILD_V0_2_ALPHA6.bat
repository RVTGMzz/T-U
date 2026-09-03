@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.4.0 - CHARACTER SKILL IDENTITY
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha640.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.4.0.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.4.0
echo Test 1: 24 NPC vanilla moi co Signature Tier 2/3 runtime that.
echo Test 2: Skill khac nhau nhung giu balance 70 Primary / 30 Secondary.
echo Test 3: Buff tam thoi het han dung, khong stack vo han va khong luu save.
echo Test 4: Moi kit vanilla hoan thien co 1 Signature icon bespoke.
echo Test 5: Equipment 6.3.1 + icons 6.3.2 + SVE/RSV + ChaCha/Tank/Vault khong regression.
echo.
explorer "%~dp0release"
pause
