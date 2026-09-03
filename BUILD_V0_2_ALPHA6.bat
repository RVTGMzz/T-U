@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.3.1 - EQUIPMENT RPG POLISH
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha631.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.3.1.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.3.1
echo Test 1: Kiem tra rarity frame tren 3 slot va tui Farmer.
echo Test 2: Hover/focus item, kiem tra Role Score + Combat Impact + Signature CD.
echo Test 3: Bam Y hoac nut TU DONG TRANG BI, kiem tra gear tot hon theo role.
echo Test 4: Kiem tra khong mat/duplicate item va save/load van dung.
echo Test 5: Passive/Signature icon + SVE/RSV skills + ChaCha/Tank/Vault regression.
echo.
explorer "%~dp0release"
pause
