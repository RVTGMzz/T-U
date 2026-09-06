@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.18 - NPC SOURCE TRUTH FIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6618Final.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.18 DIRECT BUILDER.
echo - Return Pokemon cua NPC dung source-native Pelipper bridge, khong an actor de gia Standby.
echo - Pokemon NPC con source-live van tinh la slot that cho toi khi Pelipper recall no.
echo - Dang 2/2: Invite NPC + Pokemon bat buoc Replace/Cancel va recheck ngay truoc commit.
echo - Neu replacement khong recall that su, Team Up tu choi thay vi cho 3/2.
echo - Invite NPC only van duoc neu people cap con cho; Pokemon khong tu chen vao.
echo - Player ghost-slot truth 6.6.17 va P/L+R 6.6.16 duoc giu nguyen.
echo.
echo TEST UU TIEN:
echo - Return Pokemon NPC: Pokemon phai ngung bam theo NPC that su.
echo - 2/2 roi moi NPC + Pokemon: phai Replace/Cancel, khong duoc 3/2.
echo - Moi NPC only: Pokemon source phai duoc recall trong luc NPC dang Team Up.
echo - Chay teamup_slots neu quota van lech runtime.
echo.
explorer "%~dp0release"
pause
