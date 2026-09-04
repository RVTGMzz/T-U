@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.5.3 - SURGE VALIDATION HARNESS
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha653.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.5.3.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.5.3
echo.
echo LENH TEST SURGE MOI:
echo - teamup_test surge status
 echo - teamup_test surge reapply
 echo - teamup_test surge clear
 echo - teamup_test surge board
echo.
echo TEST NHANH:
echo - status: hien Describe + LastTelemetry + so Surge monster hien tai.
echo - reapply: xoa Surge extras cu truoc, sau do apply lai dung 1 budget moi.
echo - clear: CHI xoa monster co marker Ronvotri.TeamUp/SurgeSpawn.
echo - board: test Marlon Threat Board tai AdventureGuild sau encounter.
echo - Cardcha_CardTestArena van suppression=cardcha-sandbox va khong inject Surge.
echo - Safe placement 6.5.2 + MiMi/Sudoku/Origin/equipment/Vault/51 NPC van regression lock.
echo.
explorer "%~dp0release"
pause
