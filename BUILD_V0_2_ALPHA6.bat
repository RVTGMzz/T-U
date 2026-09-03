@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.3.2 - SIGNATURE ICON ART PASS
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha632.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.3.2.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.3.2
echo Test 1: Character Profile chi co 1 icon cho Signature, Passive text-only.
echo Test 2: Abigail/Alex/Harvey/Maru/Emily co icon silhouette rieng.
echo Test 3: 24 NPC SVE/RSV Wave 1 co icon bespoke rieng.
echo Test 4: NPC chua art-pass van fallback icon an toan, khong crash.
echo Test 5: Equipment 6.3.1 + combat + ChaCha/Tank/Vault khong regression.
echo.
explorer "%~dp0release"
pause
