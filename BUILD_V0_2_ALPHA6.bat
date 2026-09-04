@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.0 - PARTY STRATEGY FOUNDATION
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha660.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.6.0.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.6.0
echo.
echo LENH PARTY STRATEGY:
echo - teamup_strategy status
echo - teamup_strategy balanced
echo - teamup_strategy defensive
echo - teamup_strategy aggressive
echo - teamup_strategy hold
echo - teamup_strategy boss
echo.
echo TEST NHANH:
echo - Defensive: radius ngan hon, heal urgency cao hon, attack cham hon nhe.
echo - Aggressive: radius xa hon, attack nhanh hon nhe.
echo - Hold: NPC khong chase target ngoai attack range.
echo - Boss: uu tien candidate co MaxHealth cao nhat.
echo - Doi strategy phai clear combat target locks sach.
echo - Surge 6.5.3 va cac regression lock cu van phai con nguyen.
echo.
explorer "%~dp0release"
pause
