@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.4.5 - INTERACTION AI EXPANSION
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha645.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.4.5.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.4.5
echo.
echo TEST NHANH:
echo - Double-click gear trong tui de trang bi; double-click gear NPC de thao.
echo - Controller Up/Down phai toi duoc TU DONG TRANG BI va THAO TRANG BI.
echo - Kho Party: giu chuot keo item qua lai giua tui va kho roi tha.
echo - Combat: dung yen trong sandbox, NPC khong quay len/xuong lien tuc va van danh quai gan minh.
echo - Ariah va cac NPC SVE/RSV con lai khong con 0/5 + ky nang cho hoan thien.
echo - Neu van co loi sound null, gui 20-30 dong log truoc stack trace.
echo.
explorer "%~dp0release"
pause
