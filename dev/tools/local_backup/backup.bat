@echo off
REM Simple batch file to run the backup script

echo Running WabbitBot Local Backup...
echo.

REM Capture all output including errors to a temp log
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0backup-solution.ps1" 2>&1 > temp_output.log

REM Always show the output
type temp_output.log

REM Always preserve temp log for inspection
echo.
echo Temp output log saved as: %~dp0temp_output.log

echo.
echo Press any key to exit...
pause > nul
