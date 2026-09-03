@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.4.1 - FRIENDSHIP ^& BOND
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha641.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.4.1.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.4.1
echo Test 1: 4 tim Trusted tang progression nhe.
echo Test 2: 8 tim Close Companion cai thien timing AI theo role.
echo Test 3: 10 tim Signature Affinity tang nhe hieu qua Signature.
echo Test 4: spouse co Bond Trait theo role; 14 tim co Soulmate Trait.
echo Test 5: relationship buff khong stack vo han va khong luu vao save.
echo.
explorer "%~dp0release"
pause
