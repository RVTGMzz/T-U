@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.4.6 - EXPANSION SIGNATURE ART BALANCE
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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha646.ps1"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Gui file BUILD_LOG.txt cho ChatGPT de sua.
  pause
  exit /b 1
)

echo.
echo BUILD OK - ALPHA 6.4.6.
echo Mo thu muc release de lay ZIP cai vao Mods.
echo SMAPI phai hien: Team Up DEBUG HARNESS READY ... 6.4.6
echo.
echo TEST NHANH:
echo - Mo profile cua Ariah, Apples, Torts, Zayne va cac NPC expansion moi.
echo - Moi NPC hoan thien phai co 1 Signature icon bespoke, khong con icon procedural fallback.
echo - Cung class nhung NPC khac nhau ve Power / Reach / Utility / Tempo.
echo - Khong NPC nao duoc cong tat ca: budget cua moi nguoi phai tong bang 0.
echo - Test Tier 2/Tier 3 trong Cardcha sandbox de so sanh DPS, Tank, Heal, Support, Control.
echo - Regression: double-click gear, controller, Party Vault drag/drop, AI anti-spin van hoat dong.
echo.
explorer "%~dp0release"
pause
