@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.14 - PELIPPER CAPTURE SAFETY
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6614.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.14 DIRECT BUILDER.
echo - Team-wide stop offensive actions khi Pelipper Pokemon cham nguong capture.
echo - Damage ceiling chan hit lon / crit vuot qua nguong capture.
echo - Heal, revive, guard va support cho phe minh van hoat dong.
echo - Neu con enemy khac, Team Up chuyen target thay vi dung ca tran.
echo - 6.6.13 single-target combat, quota 2/2, bye, water/bridge performance duoc giu regression.
echo.
echo TEST UU TIEN:
echo - 1 Pokemon hoang tren 10%%: team van danh binh thuong.
echo - Cham ~10%%: ca team ngung gay damage vao no.
echo - Lam dong doi mat mau trong luc Pokemon dang protected: healer van heal.
echo - Dat enemy khac gan Pokemon protected: AoE khong duoc lam Pokemon protected mat them mau.
echo.
explorer "%~dp0release"
pause
