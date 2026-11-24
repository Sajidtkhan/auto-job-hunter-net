# AutoJobHunter.NET ⚡
**Automated Daily Job Search & Auto-Apply Agent for .NET & SQL Roles in Dubai & Germany**

Developed with **.NET 9, C#, Entity Framework Core (SQLite / SQL Server), MailKit, and a modern Single-Page Web Dashboard**.

---

## 🌟 Key Features

1. **Daily Scheduled Worker (`DailyJobSchedulerService`)**:
   - Runs automatically every 24 hours at the configured hour (default: **9:00 AM**).
   - Scans live job sources, filters, computes relevance scores, and stores matches.
   - Prevents duplicate applications and maintains historical tracking in SQL database `jobs.db`.

2. **Live Multi-Source Job Discovery Engine**:
   - **Germany Tech Hub**: Connects to the **Arbeitnow API** (Germany's top English-speaking & visa-sponsored tech board), StepStone Germany, and Xing feeds.
   - **Dubai & UAE Tech Hub**: Integrates regional Gulf tech feeds (Naukrigulf, Bayt, GulfTalent, Dubai Internet City tech hubs, Michael Page Middle East).
   - **Global .NET Aggregators**: Connects to Jobicy, Remotive, and curated international developer openings.

3. **Intelligent Keyword & Location Matching (`JobMatchingService`)**:
   - Specifically scores jobs against **.NET** (C#, .NET 8/9, ASP.NET Core Web API, Microservices) and **SQL** (MS SQL Server, T-SQL, EF Core, Dapper, query optimization).
   - Location filtering dedicated to **Dubai / UAE** and **Germany** (Berlin, Munich, Frankfurt, Hamburg).
   - Match Score calculated from 0 to 100%.

4. **Dynamic Tailored Cover Letter Generator (`CoverLetterGeneratorService`)**:
   - **For Germany Roles**: Emphasizes enterprise Clean Architecture, CQRS, SOLID, fluent business English, and EU Blue Card / visa sponsorship eligibility.
   - **For Dubai Roles**: Emphasizes immediate availability (30-day notice / immediate), valid international passport, high-scale booking systems, multi-tenant SaaS, and SQL performance tuning.

5. **Automated Application & 1-Click Apply (`EmailApplicationService`)**:
   - Extracts recruiter / HR email addresses (`careers@...`, `jobs@...`) directly from job descriptions.
   - When enabled, automatically attaches your ATS Resume (`Sajid_Khan_Resume_2Page.html` or PDF) and sends the tailored cover letter via SMTP.
   - Default safe **Simulation Mode** (allows dry-run testing before providing your live Gmail App Password).

6. **Interactive Modern Web Dashboard**:
   - URL: `http://localhost:5000`
   - Real-time statistics (Dubai count, Germany count, applied count, match distribution).
   - Filter by Dubai 🇦🇪, Germany 🇩🇪, status, or search terms.
   - 1-Click "Scan Jobs Now" and "Scan & Auto-Apply".
   - 1-Click "View & Copy Tailored Cover Letter".
   - In-browser Candidate Profile and Settings management.

---

## 🚀 Quick Start

### 1. Launch with Batch Script:
Double-click `run.bat` in this folder:
```cmd
C:\Users\Sajid Khan\.gemini\antigravity\scratch\JobAutoApplyNET\run.bat
```
This starts the application and automatically opens your web browser to `http://localhost:5000`.

### 2. Or Launch via Command Line:
```powershell
cd "C:\Users\Sajid Khan\.gemini\antigravity\scratch\JobAutoApplyNET"
dotnet run --urls http://localhost:5000
```

---

## 📁 Project Architecture

```text
JobAutoApplyNET/
├── Program.cs                     # ASP.NET Core 9 Minimal API & Endpoint Routing
├── appsettings.json               # Application Configuration
├── jobs.db                        # SQLite Database (Auto-created on launch)
├── run.bat                        # 1-Click Windows Launcher
├── Models/
│   ├── JobPosting.cs              # Database entity for jobs (match score, country, status)
│   ├── JobApplication.cs          # Audit log for sent applications
│   ├── UserProfile.cs             # Candidate details (pre-loaded with Sajid Khan's resume)
│   └── AppConfig.cs               # Daily run hour, SMTP, Telegram settings
├── Data/
│   └── JobDbContext.cs            # EF Core DbContext with SQLite (swappable to SQL Server)
├── Services/
│   ├── IJobSource.cs              # Extensible interface for job providers
│   ├── ArbeitnowJobSource.cs      # Germany / EU tech jobs API
│   ├── DubaiTechJobSource.cs      # Dubai, UAE & Middle East jobs feed
│   ├── JobMatchingService.cs      # .NET & SQL relevance scoring engine
│   ├── CoverLetterGeneratorService.cs # AI-tailored letters for Dubai vs Germany
│   ├── EmailApplicationService.cs # Automated email sender with resume attachment
│   ├── NotificationService.cs     # Telegram and console alerts
│   ├── AggregatorJobService.cs    # Orchestrator & deduplication engine
│   └── DailyJobSchedulerService.cs# 24-hour background scheduler
└── wwwroot/
    ├── index.html                 # Modern Dashboard UI
    ├── style.css                  # Dark-mode styling with Tailwind aesthetics
    └── app.js                     # Interactive frontend controller
```

---

## ⚙️ Configuration

### Outgoing Email (For live auto-apply):
In the **Settings** tab of the dashboard (or via `appsettings.json`):
- **Sender Email**: `sajidkhanbarun@gmail.com`
- **SMTP Host**: `smtp.gmail.com`
- **SMTP Port**: `587`
- **Password**: Google Account App Password (generate at `https://myaccount.google.com/apppasswords`)
*(Leaving the password blank keeps the app in Simulation Mode so you can safely review before sending).*

### Telegram Alerts (Optional):
- Create a bot via `@BotFather` on Telegram.
- Enter your **Bot Token** and **Chat ID** in the Settings tab.
- You will receive instant notifications on your phone whenever new .NET & SQL jobs are discovered in Dubai and Germany.
