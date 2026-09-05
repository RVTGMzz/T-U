@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.10 - PELIPPER FOLLOW AUTHORITY
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6610.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.10 DIRECT BUILDER.
echo - Pelipper Pokemon bi skip truoc khi FollowService resolve actor.
echo - Team Up khong con Prepare/Hold/Follow/Warp/Halt/controller Pokemon Pelipper.
echo - Soft Active/Standby marker chi ghi khi gia tri thuc su thay doi.
echo - HP HUD/overhead bar 6.6.9 duoc giu nguyen.
echo - Fix performance 6.6.7 va land-safe 6.6.8 duoc giu nguyen.
echo.
echo TEST UU TIEN:
echo - Goi Pokemon Farmer ra, dung/chay/warp 30 giay: khong nhap nhay.
echo - Recruit NPC + Pokemon, dung/chay/warp 30 giay: khong nhap nhay.
echo - Farmer Pokemon + NPC Pokemon cung luc: ca hai phai on dinh.
echo - Thu lai song/cau: van muot va NPC nguoi khong xuong nuoc.
echo - De NPC mat mau: HP HUD va overhead bar van hoat dong.
echo.
explorer "%~dp0release"
pause
