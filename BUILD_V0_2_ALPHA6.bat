@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.2 - PARTY TACTICS + CAPACITY UI
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha662.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.6.2.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.6.2
echo.
echo TACTICS UI:
echo - Mo Codex ^> Tactics / Chien thuat.
echo - Xem People used/max va Combat Companions used/max.
echo - Chon 5 strategy bang mouse / keyboard / controller.
echo.
echo MULTIPLAYER STRATEGY:
echo - Host la authority.
echo - Farmhand doi strategy se gui request cho host.
echo - Host apply, clear combat locks va sync strategy lai cac client.
echo.
echo LUAT PARTY GIU NGUYEN:
echo - Tong Farmer online + NPC active toi da 6 nguoi.
echo - External Pokemon/summon combat dung chung toi da 2.
echo - Vanilla pet va ChaCha khong chiem companion slot.
echo.
echo TEST NHANH:
echo - Codex footer co nut Tactics va controller focus dung.
echo - Doi tung strategy va kiem tra UI highlight.
echo - 2 client: farmhand doi strategy, host va farmhand phai dong bo.
echo - Re-test cap 6 nguoi, 2 companion, Sudoku handshake va Surge regression.
echo.
explorer "%~dp0release"
pause
