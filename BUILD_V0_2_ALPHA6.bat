@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.4.4 - TEST FEEDBACK HOTFIX
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha644.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.4.4.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.4.4
echo.
echo TEST NHANH:
echo - Equipment: chon Trinket truoc, sau do click Liem. Phai tu chuyen sang Weapon va equip ngay.
echo - Auto Equip xong bam X phai lay lai item; bam X tiep co the thao slot khac dang co gear.
echo - Footer tui do phai ro, hover card khong che footer.
echo - Profile: Moi quan he nam trong vung scroll ben phai, khong roi xuong nut footer.
echo - Ariah/pending kit: text pending khong bi phong x2.
echo - Healer: farmerhp 40%% + waves normal de xem HEAL +X ro hon.
echo - Cardcha sandbox van dung Cardcha_CardTestArena.
echo.
explorer "%~dp0release"
pause
