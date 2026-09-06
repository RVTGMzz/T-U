@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.15 - COMPANION INTENT + RECALL
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6615.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.15 DIRECT BUILDER.
echo - NPC-only la lua chon ben vung, reconcile khong duoc tu them Pokemon linked.
echo - Pokemon Standby co Call/Return va replacement flow khi pool 2/2 day.
echo - Player re-summon Pokemon Standby khong bi Team Up nuot nguoc im lang.
echo - Capture floor clamp Monster.takeDamage de Pokemon phe minh khong ket lieu wild Pokemon duoi nguong.
echo - Heal/buff/revive/guard van hoat dong khi target dang capture-protected.
echo.
echo TEST UU TIEN:
echo - Recruit NPC + chon NPC only khi dang co Pokemon cua Farmer.
echo - Noi chuyen NPC, dung Party Menu key de Call/Return Pokemon linked.
echo - 2/2 roi summon Pokemon thu 3: phai hoi Replace/Cancel.
echo - Bat 10%% capture mode, cho ca NPC va Pokemon companion cung danh wild Pokemon.
echo.
explorer "%~dp0release"
pause
