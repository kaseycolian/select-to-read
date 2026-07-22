@echo off
REM Double-click this file to build SelectSpeak and put a shortcut on your Desktop.
REM Pass /startup to also launch SelectSpeak automatically at sign-in.
setlocal
set ARGS=
if /I "%~1"=="/startup" set ARGS=-StartWithWindows
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup.ps1" %ARGS%
echo.
pause
