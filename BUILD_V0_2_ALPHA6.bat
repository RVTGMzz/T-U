@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.11 - NO COMPANION PROFILES
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha6611.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.11 DIRECT BUILDER.
echo - Pokemon, summon va external companion khong con mo Ho So Nhan Vat.
echo - Dialogue companion khong con hien hint Ho So cua Team Up.
echo - ProfileKey va OpenProfileFromDialogue deu co guard companion/summon.
echo - Pelipper follow authority 6.6.10 duoc giu nguyen.
echo - HP UI 6.6.9, land-safe 6.6.8 va performance 6.6.7 duoc giu nguyen.
echo.
echo TEST UU TIEN:
echo - Noi chuyen Rowlet/Pokemon: khong co hint Ho So, bam Profile cung khong mo.
echo - PelipperTown.Villager.* / Player.* khong duoc mo placeholder Special / Companion.
echo - NPC nguoi, MiMi, Sudoku va Codex catalog van mo Ho So binh thuong.
echo - Pokemon van khong nhap nhay theo fix 6.6.10.
echo.
explorer "%~dp0release"
pause
