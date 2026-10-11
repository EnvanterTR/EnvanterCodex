@echo off
setlocal
powershell.exe -NoLogo -NoProfile -NoExit -ExecutionPolicy Bypass -File "%~dp0setup.ps1" -PersonalSinglePc
endlocal
