@echo off
title Push AutoJobHunter.NET to GitHub for 24/7 Cloud Hosting
echo =================================================================
echo   AutoJobHunter.NET - 24/7 Permanent Cloud Deployment Setup
echo =================================================================
echo.
echo Please create a new GitHub repository at: https://github.com/new
echo (Name it: JobAutoApplyNET)
echo.
set /p REPO_URL="Enter your GitHub Repository URL (e.g., https://github.com/username/JobAutoApplyNET.git): "

if "%REPO_URL%"=="" (
    echo No URL provided. Aborted.
    pause
    exit /b
)

echo.
echo Setting remote origin...
git remote remove origin 2>nul
git remote add origin %REPO_URL%
git branch -M main

echo.
echo Pushing code to GitHub...
git push -u origin main

echo.
echo =================================================================
echo Repository pushed successfully!
echo.
echo Next Steps to make it a 24/7 live website:
echo 1. Go to https://dashboard.render.com (Free)
echo 2. Click "New +" -> "Web Service"
echo 3. Connect your GitHub repository "JobAutoApplyNET"
echo 4. Click "Create Web Service"
echo.
echo Render will give you a permanent 24/7 live URL (e.g., https://autojobhunter-net.onrender.com)
echo and GitHub Actions will run daily auto-apply in the cloud at 9:00 AM!
echo =================================================================
pause
