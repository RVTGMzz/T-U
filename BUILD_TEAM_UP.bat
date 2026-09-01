@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo   Team Up! - build v0.1 prototype
echo ========================================
echo.

dotnet restore .\src\TeamUp\TeamUp.csproj
if errorlevel 1 goto :fail

dotnet build .\src\TeamUp\TeamUp.csproj -c Release
if errorlevel 1 goto :fail

echo.
echo Build complete.
echo The Stardew ModBuildConfig package will also create a release zip in the project bin folder.
echo.
pause
exit /b 0

:fail
echo.
echo BUILD FAILED. Copy the full console output when reporting the error.
echo.
pause
exit /b 1
