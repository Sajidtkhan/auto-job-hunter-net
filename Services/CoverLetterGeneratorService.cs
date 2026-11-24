using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Services;

public class CoverLetterGeneratorService
{
    public string GenerateCoverLetter(JobPosting job, UserProfile profile)
    {
        bool isGermany = job.Country.Equals("Germany", StringComparison.OrdinalIgnoreCase) ||
                         job.Location.Contains("Germany", StringComparison.OrdinalIgnoreCase) ||
                         job.Location.Contains("Berlin", StringComparison.OrdinalIgnoreCase) ||
                         job.Location.Contains("Munich", StringComparison.OrdinalIgnoreCase);

        bool isDubai = job.Country.Contains("Dubai", StringComparison.OrdinalIgnoreCase) ||
                        job.Country.Contains("UAE", StringComparison.OrdinalIgnoreCase) ||
                        job.Location.Contains("Dubai", StringComparison.OrdinalIgnoreCase);

        if (isGermany)
        {
            return $@"Dear Hiring Team at {job.Company},

I am writing to express my strong enthusiasm for the {job.Title} role in Germany. With over 6 years of proven backend engineering experience specialized in C#, .NET 8, ASP.NET Core, and Microsoft SQL Server, I am confident in delivering high performance and resilient architecture to your engineering team.

Key Highlights of My Experience:
• Enterprise .NET & Microservices: Architected high-throughput REST APIs and scalable microservices following Clean Architecture, CQRS (MediatR), and Domain-Driven Design (DDD).
• Database Mastery & SQL Tuning: Advanced mastery of MS SQL Server, T-SQL, EF Core, and Dapper; optimized complex queries and indexing strategies that reduced database deadlocks and boosted query execution speeds by 30%.
• Distributed Caching & Reliability: Implemented Redis distributed caching handling 100,000+ daily transactions with sub-second response latency.
• International Mobility & English: I have full professional working proficiency in English, hold a valid international passport, and am fully qualified for an EU Blue Card work authorization with immediate/30-day onboarding availability.

I would welcome the opportunity to discuss how my technical leadership in .NET and SQL can contribute to {job.Company}'s backend initiatives.

Sincerely,
{profile.FullName}
Senior .NET Core Backend Developer
Phone: {profile.Phone} | Email: {profile.Email}
LinkedIn: {profile.LinkedInUrl}";
        }
        else if (isDubai)
        {
            return $@"Dear Hiring Manager at {job.Company},

I am writing to formally apply for the {job.Title} position in Dubai, UAE. Having monitored {job.Company}'s growth, I am eager to bring my 6+ years of specialized expertise in .NET Core (C#), ASP.NET Core Web API, and Enterprise MS SQL Server to your engineering department.

Why I Am an Immediate Fit for Your Dubai Team:
• High-Concurrency Ecosystems: Successfully built booking and SaaS dispatch engines handling high-traffic commercial peak loads with sub-second API latency.
• Advanced SQL Server & Performance Optimization: Extensive experience formulating indexing strategies, execution plan tuning, and partitioning in SQL Server and PostgreSQL, reducing CPU utilization by 35%.
• Multi-Tenant SaaS & Security: Architected secure multi-tenant SaaS platforms with Role-Based Access Control (RBAC), JWT authentication, and OWASP Top 10 standards.
• Immediate Relocation to Dubai: I hold a valid international passport ready for immediate travel, require no local notice delays (30-day notice period / immediate start), and am fully committed to relocating to Dubai.

Thank you for your time and consideration. I look forward to the opportunity to connect for an interview.

Warm regards,
{profile.FullName}
Senior .NET Core Backend Developer
Phone: {profile.Phone} | Email: {profile.Email}
LinkedIn: {profile.LinkedInUrl}";
        }
        else
        {
            return $@"Dear Hiring Team at {job.Company},

I am excited to apply for the {job.Title} role. As a Senior .NET Core Backend Developer with over 6 years of experience building scalable backend platforms using C#, ASP.NET Core Web API, and MS SQL Server, my skills directly align with your requirements.

Technical Strengths:
• C#, .NET 8 / .NET Core, ASP.NET Core Web API, Clean Architecture, CQRS
• MS SQL Server, PostgreSQL, Redis Distributed Caching, Dapper, EF Core
• Scalable Microservices, Docker, CI/CD automated deployments
• 30-day notice period, open to remote and global relocation

I look forward to discussing how my backend engineering experience can drive value for {job.Company}.

Best regards,
{profile.FullName}
Phone: {profile.Phone} | Email: {profile.Email}
LinkedIn: {profile.LinkedInUrl}";
        }
    }
}
