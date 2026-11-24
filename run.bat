@echo off
title AutoJobHunter.NET - Dubai & Germany .NET/SQL Auto-Applier
echo ========================================================
echo    AutoJobHunter.NET - Daily Automated Job Apply Agent
echo    Target: Dubai (UAE) & Germany | Stack: .NET 9 & SQL
echo ========================================================
echo.
cd /d "%~dp0"
echo Starting AutoJobHunter.NET on http://localhost:5000 ...
timeout /t 2 /nobreak >nul
start http://localhost:5000
if exist "bin\Debug\net9.0\JobAutoApplyNET.exe" (
    bin\Debug\net9.0\JobAutoApplyNET.exe
) else (
    dotnet run --no-launch-profile --urls http://localhost:5000
)
pause
