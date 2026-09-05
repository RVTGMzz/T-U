@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.1 - SHARED PARTY MULTIPLAYER
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha661.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.6.1.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.6.1
echo.
echo LUAT PARTY:
echo - Tong Farmer dang online + NPC active = toi da 6 nguoi.
echo - Pokemon/summon combat dung chung = toi da 2.
echo - Vanilla pet va ChaCha khong chiem 2 slot companion.
echo - NPC co companion: NPC only / NPC + companion / Cancel.
echo.
echo MULTIPLAYER:
echo - Host giu party state chinh va validate request.
echo - NPC theo dung Farmer da recruit bang RecruiterId.
echo - Farmhand join co the day NPC overflow ve Inactive, khong xoa roster.
echo.
echo TEST NHANH:
echo - Single: Farmer + 5 NPC = 6/6; NPC tiep theo phai bi chan.
echo - 2 Farmer: chi 4 NPC active toi da.
echo - 2/2 companion: thu recruit NPC + companion de test replacement flow.
echo - Sudoku: PartyControlled=true khi Follow, xoa marker khi Leave.
echo - Re-test 5 Party Strategy va Surge 6.5.3 regression locks.
echo.
explorer "%~dp0release"
pause
