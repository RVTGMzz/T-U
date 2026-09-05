@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.3 - LIVE TEST HOTFIX
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha663.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.6.3.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.6.3
echo.
echo 4 LIVE HOTFIX:
echo - Controller: A trang bi item dang focus hoac item duoi controller cursor.
echo - Codex: right analog scroll dong bo selection; left analog di 2 hang, D-pad di 1 hang.
echo - Pelipper Town Pokemon: NPC/Farmer Pokemon dung chung hard cap 2/2.
echo - Water/bridge: giam pathfinding churn tren map nuoc, cau va duong hep.
echo.
echo LUAT PARTY GIU NGUYEN:
echo - Tong Farmer online + NPC active toi da 6 nguoi.
echo - External Pokemon/summon combat dung chung toi da 2.
echo - Vanilla pet va ChaCha khong chiem companion slot.
echo.
echo PELIPPER DEBUG:
echo - teamup_pelipper status
echo - teamup_pelipper reconcile
echo.
echo TEST NHANH:
echo - Equipment: D-pad/left analog chon item, A mot lan phai equip.
echo - Equipment: right analog dua cursor len item, A mot lan phai equip dung item do.
echo - Codex: scroll sau bang right analog roi bam left analog, list khong duoc nhay nguoc.
echo - Recruit 3 NPC co Pokemon: chi toi da 2 Pokemon deployed, con thu 3 phai replacement/Standby.
echo - Di qua cau/bo song voi party dong de so sanh lag voi 6.6.2.
echo.
explorer "%~dp0release"
pause
