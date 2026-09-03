@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.4.2 - UI READABILITY PASS
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha642.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.4.2.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.4.2
echo Test 1: Mo Character Profile, mo ta NOI TAI va KY NANG DAC TRUNG phai lon x2.
echo Test 2: Man hinh ngan van khong tran footer; dung mouse wheel hoac Up/Down de cuon.
echo Test 3: Controller D-pad/Left Stick len-xuong cuon mo ta; trai-phai van chon footer.
echo Test 4: Passive van chi co text; Signature van dung 1 icon.
echo Test 5: Friendship/Bond 6.4.1 va Equipment/Skill khong regression.
echo.
explorer "%~dp0release"
pause
