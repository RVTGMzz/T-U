@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.13 - SINGLE TARGET + QUOTA + BYE
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6613.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.13 DIRECT BUILDER.
echo - Single Pelipper wild/battle target duoc Team Up acquire dung.
echo - Shared combat companion cap giu hard 2/2; Pelipper Standby bi chan deploy/render.
echo - NPC den curfew noi 1 cau bye theo tinh cach roi moi ve schedule goc.
echo - Water/bridge performance, land-safe, HP bar, Switch input va Codex duoc giu regression.
echo.
echo TEST UU TIEN:
echo - Chi 1 Pokemon hoang: Tank/DPS/Control phai vao combat.
echo - Goi hon 2 Pokemon: chi 2 companion duoc xem la dang deploy.
echo - NPC den gio ve: noi 1 cau bye, khong spam, roi ve nha.
echo - Test lai cau/song: khong lag, NPC khong roi xuong nuoc, Pokemon active khong flicker.
echo.
explorer "%~dp0release"
pause
