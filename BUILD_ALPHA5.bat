@echo off
setlocal
cd /d "%~dp0"

call "%~dp0BUILD_TEAM_UP.bat"
exit /b %errorlevel%
