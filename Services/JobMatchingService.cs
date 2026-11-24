using System.Text.RegularExpressions;
using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Services;

public class JobMatchingService
{
    private static readonly string[] PrimaryKeywords = new[]
    {
        ".net", "c#", "csharp", "asp.net", ".net core", "dotnet"
    };

    private static readonly string[] SqlKeywords = new[]
    {
        "sql", "sql server", "t-sql", "tsql", "ms sql", "entity framework", "ef core", "dapper", "database"
    };

    private static readonly string[] BackendKeywords = new[]
    {
        "backend", "microservices", "web api", "rest api", "clean architecture", "cqrs", "redis", "docker"
    };

    private static readonly string[] GermanyLocations = new[]
    {
        "germany", "deutschland", "berlin", "munich", "münchen", "frankfurt", "hamburg", "cologne", "köln", "stuttgart", "düsseldorf"
    };

    private static readonly string[] DubaiLocations = new[]
    {
        "dubai", "uae", "united arab emirates", "abu dhabi", "sharjah"
    };

    public (int score, string matched, string country, string? email) EvaluateJob(string title, string company, string location, string description)
    {
        var text = $"{title} {location} {description}".ToLowerInvariant();
        var matched = new List<string>();
        int score = 0;

        // 1. Primary Keyword (.NET / C#)
        bool hasDotNet = false;
        foreach (var kw in PrimaryKeywords)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(kw)}\b", RegexOptions.IgnoreCase))
            {
                hasDotNet = true;
                matched.Add(kw.ToUpperInvariant());
            }
        }
        if (hasDotNet) score += 40;

        // 2. Database Keywords (SQL / SQL Server)
        bool hasSql = false;
        foreach (var kw in SqlKeywords)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(kw)}\b", RegexOptions.IgnoreCase))
            {
                hasSql = true;
                matched.Add(kw.ToUpperInvariant());
            }
        }
        if (hasSql) score += 25;

        // 3. Backend & Architecture Keywords
        foreach (var kw in BackendKeywords)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(kw)}\b", RegexOptions.IgnoreCase))
            {
                score += 3;
                matched.Add(kw);
            }
        }
        score = Math.Min(score, 80);

        // 4. Location Check (Germany vs Dubai)
        string detectedCountry = "Global / Remote";
        bool isGermany = GermanyLocations.Any(loc => text.Contains(loc));
        bool isDubai = DubaiLocations.Any(loc => text.Contains(loc));

        if (isGermany)
        {
            detectedCountry = "Germany";
            score += 20;
        }
        else if (isDubai)
        {
            detectedCountry = "Dubai / UAE";
            score += 20;
        }
        else if (text.Contains("remote"))
        {
            detectedCountry = "Remote";
            score += 10;
        }

        // Cap at 100
        score = Math.Clamp(score, 0, 100);

        // 5. Detect HR / apply email address
        string? detectedEmail = null;
        var emailMatch = Regex.Match(description, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");
        if (emailMatch.Success)
        {
            var emailVal = emailMatch.Value.ToLowerInvariant();
            if (!emailVal.EndsWith(".png") && !emailVal.EndsWith(".jpg") && !emailVal.Contains("example.com"))
            {
                detectedEmail = emailMatch.Value;
            }
        }

        return (score, string.Join(", ", matched.Distinct()), detectedCountry, detectedEmail);
    }
}
