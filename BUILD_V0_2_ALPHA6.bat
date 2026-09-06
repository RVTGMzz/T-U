@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.16 - CONTROLLER COMPANION FIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6616.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.16 DIRECT BUILDER.
echo - Keyboard P / controller L+R mo menu Pokemon cua NPC Team Up.
echo - L+R duoc uu tien truoc L=Profile va R=Leave, khong the kick NPC.
echo - NPC-only dang ky partner vao Standby ngay neu actor da co.
echo - Partner Pelipper dang an van duoc detect neu co owner metadata ro rang.
echo - Call/Return va Replace/Cancel giu pool 2/2.
echo - Capture floor nhan wild Pelipper truc tiep, khong cho CombatTarget marker.
echo.
echo TEST UU TIEN:
echo - Dialogue NPC member: L+R phai mo Pokemon, khong mo Leave.
echo - L rieng = Profile; R rieng = Leave sau delay rat ngan.
echo - NPC only: Pokemon linked khong tu chiem slot.
echo - Standby partner: P/L+R Call; 2/2 phai hoi Replace/Cancel.
echo - Pelipper 10%%: wild Pokemon khong bi Team Up danh xuyen nguong.
echo.
explorer "%~dp0release"
pause
