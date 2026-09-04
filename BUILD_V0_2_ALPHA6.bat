@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.5.2 - SURGE RUNTIME POLISH
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha652.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.5.2.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.5.2
echo.
echo TEST NHANH:
echo - Vao Mines/Cave co quai, doi khoang 1 giay: Surge spawn tren tile clear/placeable.
echo - Map hep co the spawn it hon x2, nhung KHONG duoc nhai quai vao tuong/vat can.
echo - SMAPI log co [SurgeTelemetry] baseline/wanted/spawned/unsafeRejected/threat/suppression.
echo - Cardcha_CardTestArena van suppression=cardcha-sandbox, khong inject Surge.
echo - Quay ve AdventureGuild sau encounter: Marlon Threat Board hien 1 lan neu khong co dialogue/event/menu.
echo - Regression MiMi gate + Sudoku + Origin + equipment + Party Vault + 51 NPC expansion.
echo.
explorer "%~dp0release"
pause
