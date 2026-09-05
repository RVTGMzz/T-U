@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.9 - FLICKER + THIN HP HOTFIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha669.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.9 DIRECT BUILDER.
echo - Team Up khong con tranh IsInvisible/Halt/controller cua Pokemon Pelipper.
echo - Pelipper chi nhan soft deployment marker Active/Standby tu Team Up.
echo - Party HUD HP: bar mong 5px, ten NPC nho o mep trai giua man hinh.
echo - Overhead HP: bar mong 4px, chi hien khi mat mau/combat/downed.
echo - Host sync HP/downed/state cho farmhand khi co thay doi.
echo - Fix performance 6.6.7 va land-safe 6.6.8 duoc giu nguyen.
echo.
echo TEST UU TIEN:
echo - Pokemon cua Farmer + Pokemon cua Alex dung 20-30 giay: khong duoc chop tat.
echo - De NPC mat mau: HUD va overhead bar phai giam dung ti le.
echo - Full HP ngoai combat: overhead phai an.
echo - Thu lai doan song/cau: van muot va NPC khong xuong nuoc.
echo - Test Switch equip/unequip va Codex 1 profile/input.
echo.
explorer "%~dp0release"
pause