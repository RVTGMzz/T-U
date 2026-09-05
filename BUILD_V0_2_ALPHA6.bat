@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.4 - CONTROLLER EQUIPMENT HOTFIX
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha664.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.6.4.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.6.4
echo.
echo CONTROLLER EQUIPMENT HOTFIX:
echo - Moi nut A/X/Y chi duoc xu ly mot lan moi thao tac.
echo - Chan click chuot ao do gamepad A sinh ra.
echo - Slot NPC khong con double-click de thao do, tranh ghost equip/unequip.
echo - Chi bao "Da trang bi" sau khi ca metadata va equipment storage deu commit that.
echo - Chi bao "Da thao" sau khi slot that su da trong.
echo - Double-click item trong tui 450ms van duoc giu.
echo.
echo REGRESSION GIU NGUYEN TU 6.6.3:
echo - Codex analog scroll/selection sync.
echo - Pelipper Town Pokemon dung chung hard cap 2/2.
echo - Water/bridge follower performance hotfix.
echo - Tactics 5 strategy, Sudoku, MiMi, Surge va Party Vault.
echo.
echo TEST NHANH:
echo - Chon vu khi trong tui bang controller, A mot lan phai equip va slot hien item that.
echo - Khong duoc xuat hien cap thong bao "Da trang bi" roi "Da thao" cho cung mot lan A.
echo - Thu giu/spam A ngan: khong duoc lap trang thai equip/unequip.
echo - Right analog dua cursor len item, A mot lan phai equip dung item.
echo - X van thao do binh thuong; nut Thao trang bi van hoat dong.
echo.
explorer "%~dp0release"
pause
