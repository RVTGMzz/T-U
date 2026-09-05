@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.7 - WATER COMBAT PATH HOTFIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha667.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.7.
echo - Pelipper water/decorative actors are not combat targets by default.
echo - Combat tile search uses lightweight map/passable checks.
echo - Unreachable combat paths wait 24 ticks before retrying.
echo - Combat movement path creation is pulsed every 3 ticks.
echo - Switch semantic equip/unequip remains preserved.
echo - Codex one-profile-per-input remains preserved.
echo.
echo TEST UU TIEN:
echo - Di lai dung doan cau/bo song tung lag nang voi party dong.
echo - Thu ca ban ngay va ban dem neu khu vuc co Pokemon hoang da.
echo - Kiem tra NPC van danh quai that binh thuong tren dat.
echo - Kiem tra Switch equip va unequip khong regress.
echo.
explorer "%~dp0release"
pause