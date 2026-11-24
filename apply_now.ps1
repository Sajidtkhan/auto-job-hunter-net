# Auto Apply Runner Script for Dubai & Germany .NET & SQL Roles
$ErrorActionPreference = "Continue"

$appDir = "C:\Users\Sajid Khan\.gemini\antigravity\scratch\JobAutoApplyNET"
$exePath = Join-Path $appDir "bin\Debug\net9.0\JobAutoApplyNET.exe"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " AutoJobHunter.NET - Executing Live Apply for Dubai & Germany" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Start application if not already running on port 5000
$existing = Get-Process -Name "JobAutoApplyNET" -ErrorAction SilentlyContinue
if (-not $existing) {
    Write-Host "[1/4] Starting AutoJobHunter background service..." -ForegroundColor Yellow
    $proc = Start-Process -FilePath $exePath -WorkingDirectory $appDir -PassThru -NoNewWindow
    Start-Sleep -Seconds 3
} else {
    Write-Host "[1/4] AutoJobHunter service is already active (PID: $($existing.Id))." -ForegroundColor Yellow
}

# 2. Trigger Scan & Auto-Apply
Write-Host "[2/4] Triggering job scan across Dubai & Germany tech portals..." -ForegroundColor Yellow
$scanRes = Invoke-RestMethod -Uri "http://localhost:5000/api/jobs/scan?autoApply=true" -Method Post
Write-Host "   -> Analyzed $($scanRes.totalDiscovered) jobs from Arbeitnow, Jobicy & Gulf Tech feeds." -ForegroundColor Gray
Write-Host "   -> Found $($scanRes.newMatchesFound) new matches (Dubai: $($scanRes.dubaiCount), Germany: $($scanRes.germanyCount))." -ForegroundColor Green

# 3. Apply to all matching unapplied jobs
Write-Host "[3/4] Dispatching tailored applications to all matching roles..." -ForegroundColor Yellow
$applyRes = Invoke-RestMethod -Uri "http://localhost:5000/api/jobs/apply-all" -Method Post
Write-Host "   -> Dispatched applications for $($applyRes.count) matching positions." -ForegroundColor Green

# 4. Fetch Summary & Applied Jobs
Write-Host "[4/4] Generating Application Summary Report..." -ForegroundColor Yellow
$stats = Invoke-RestMethod -Uri "http://localhost:5000/api/stats"
$appliedJobs = Invoke-RestMethod -Uri "http://localhost:5000/api/jobs?status=Applied"

Write-Host "`n================ APPLICATION SUMMARY ================" -ForegroundColor Cyan
Write-Host "Total Jobs in Database : $($stats.totalJobs)"
Write-Host "Dubai / UAE Jobs       : $($stats.dubaiJobs)"
Write-Host "Germany Jobs           : $($stats.germanyJobs)"
Write-Host "Total Applied Jobs     : $($stats.appliedJobs)"
Write-Host "=====================================================" -ForegroundColor Cyan

# Generate Markdown Report
$reportFile = "C:\Users\Sajid Khan\.gemini\antigravity\scratch\JobAutoApplyNET\Job_Application_Report.md"
$report = @"
# ⚡ AutoJobHunter.NET Application Dispatch Report
**Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")  
**Candidate:** Sajid Tavrej Khan (Senior .NET Core & SQL Developer)  
**Target Locations:** Dubai (UAE) & Germany  

---

## 📊 Summary Statistics
- **Total Tracked Jobs:** $($stats.totalJobs)
- **Dubai / UAE Roles:** $($stats.dubaiJobs)
- **Germany Roles:** $($stats.germanyJobs)
- **Total Successfully Applied:** $($stats.appliedJobs)
- **Live Dashboard:** [http://localhost:5000](http://localhost:5000)

---

## 💼 Applied Positions Details

| # | Match | Role & Company | Location | Tech Stack | Status / Notes |
|---|:-----:|----------------|----------|------------|----------------|
"@

$i = 1
foreach ($j in $appliedJobs) {
    $report += "`n| $i | **$($j.matchScore)%** | **$($j.title)**<br>*$($j.company)* | $($j.location) ($($j.country)) | ``$($j.matchedKeywords)`` | $($j.status)<br><small>$($j.notes)</small> |"
    $i++
}

$sampleDubaiLetter = ($appliedJobs | Where-Object { $_.country -like '*Dubai*' } | Select-Object -First 1).generatedCoverLetter
$sampleGermanyLetter = ($appliedJobs | Where-Object { $_.country -like '*Germany*' } | Select-Object -First 1).generatedCoverLetter

$report += "`n`n---`n`n## 📝 Sample Tailored Cover Letter (Dubai Position)`n````text`n$sampleDubaiLetter`n`````n`n---`n`n## 📝 Sample Tailored Cover Letter (Germany Position)`n````text`n$sampleGermanyLetter`n`````n"

Set-Content -Path $reportFile -Value $report -Encoding UTF8
Write-Host "Report saved to: $reportFile" -ForegroundColor Green
