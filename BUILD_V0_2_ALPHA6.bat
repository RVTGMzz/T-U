@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.8 - LAND-SAFE FOLLOW HOTFIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha668.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.8.
echo - NPC nguoi khong nhan bare-water tile lam formation/combat target.
echo - Cau that tren water Back-layer van duoc phep neu co Buildings overlay.
echo - NPC dang mac duoi nuoc duoc rescue ve formation tile an toan.
echo - Neu Farmer o traversal surface khong co land gan do, NPC cho tren tile an toan thay vi lao xuong nuoc.
echo - Performance hotfix 6.6.7 duoc giu nguyen.
echo - Pokemon/summon khong bi ep dung quy tac land-only cua NPC nguoi.
echo.
echo TEST UU TIEN:
echo - Quay lai dung doan song trong anh va chay 3 vong voi party dong.
echo - NPC phai o tren bo/cau va FPS van muot nhu 6.6.7.
echo - Thu mot cau that de dam bao bridge overlay khong bi chan.
echo - Test Switch equip/unequip va Codex 1 profile/input.
echo.
explorer "%~dp0release"
pause
