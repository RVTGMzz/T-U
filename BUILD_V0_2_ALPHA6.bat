@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.17 - PELIPPER SLOT TRUTH FIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6617.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.17 DIRECT BUILDER.
echo - Pokemon Farmer da Pelipper recall/swap se tu nhả ghost slot Team Up.
echo - Pokemon Farmer dang live la source truth, khong doi IsSummoned/IsDeployed reflection nua.
echo - Moi farmer chi giu 1 active Pelipper partner lane theo runtime source.
echo - Team Up khong dung render-only invisibility lam quota fallback nua.
echo - Active/Waiting/ReturningHome van giu slot; Standby/Inactive khong giu slot.
echo - P / L+R Pokemon NPC va controller routing 6.6.16 duoc giu nguyen.
echo.
echo TEST UU TIEN:
echo - Chi moi 1 NPC, goi Pokemon Farmer phai ra neu pool chua that su 2/2.
echo - Recall A, goi B: A phai Standby va nhả slot ngay.
echo - Swap A sang B khi co 1 Pokemon NPC active: B thay dung slot cua A, khong thanh 3/2 ao.
echo - Chay teamup_slots de xem reserved va sourceLive.
echo.
explorer "%~dp0release"
pause
