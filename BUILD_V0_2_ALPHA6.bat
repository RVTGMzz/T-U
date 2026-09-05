@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.12 - CURFEW + CONTEXT HEALTH
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6612.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.12 DIRECT BUILDER.
echo - NPC co curfew theo do than thiet: 23:00 -> gan 03:00.
echo - Het gio thi NPC tra ve schedule goc, linked companion ve Standby.
echo - Bo HUD mau cot ben trai.
echo - Thanh mau nam duoi chan NPC, hien theo combat/damage 3s/noi chuyen.
echo - Pelipper authority, performance, land-safe va no-companion-profile duoc giu nguyen.
echo.
echo TEST UU TIEN:
echo - NPC 0-2 tim ve luc 23:00; NPC than thiet cao o lai muon hon.
echo - NPC + Pokemon: khi NPC ve nha Pokemon linked ve Standby.
echo - Gay sat thuong NPC: thanh mau duoi chan hien ~3 giay roi an.
echo - Noi chuyen NPC: thanh mau duoi chan hien; khong co HUD mau ben trai.
echo - Song/cau van muot, NPC khong roi xuong nuoc, Pokemon khong flicker.
echo.
explorer "%~dp0release"
pause
