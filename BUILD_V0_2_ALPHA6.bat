@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.3.0 - NPC LOADOUT + TRAIT ICONS
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha63.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.3.0.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.3.0
echo Test 1: Mo Trang bi NPC, kiem tra portrait + 3 slot + luoi tui Farmer.
echo Test 2: Chon Weapon/Armor/Trinket, item khong hop phai bi lam mo.
echo Test 3: Mo Codex profile, Passive va Signature phai co 2 icon rieng.
echo Test 4: SVE/RSV skill Wave 1 van hoat dong nhu Alpha 6.2.
echo.
explorer "%~dp0release"
pause
