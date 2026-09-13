@echo off
setlocal
if "%KMC_PATH%"=="" set KMC_PATH=C:\Program Files (x86)\Steam\steamapps\common\CarX Drift Racing Online\kino\dev\CockpitFreeLook_maykr.kmc
set DLL=%~dp0bin\Release\netstandard2.1\CockpitFreeLook.dll
set OUT=%~dp0build
if not exist "%OUT%" mkdir "%OUT%"
"%~dp0..\tools\maykr.exe" "%DLL%" -c "%KMC_PATH%" -o "%OUT%" -np
endlocal
