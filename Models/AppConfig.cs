namespace JobAutoApplyNET.Models;

public class AppConfig
{
    public List<string> TargetKeywords { get; set; } = new() 
    { 
        ".NET", "C#", "ASP.NET", "ASP.NET Core", "SQL", "SQL Server", "EF Core", "Entity Framework", "Backend Developer", "Microservices" 
    };

    public List<string> TargetCountries { get; set; } = new() 
    { 
        "Germany", "Dubai", "United Arab Emirates", "UAE" 
    };

    public List<string> BlacklistedCompanies { get; set; } = new()
    {
        "red orange",
        "red orange technologies",
        "redorangetechnologies",
        "redorangetechnologies.com",
        "arethos",
        "arethos ventures",
        "arethos.com",
        "arerhos",
        "arerhos ventures",
        "arerhosventures",
        "revclerx",
        "revclerx.com",
        "maxval",
        "maxval group",
        "maxval.com",
        "mindmatrix",
        "mindmatrix.net"
    };

    public bool IsBlacklisted(string? company, string? url = null, string? email = null)
    {
        if (BlacklistedCompanies == null || BlacklistedCompanies.Count == 0) return false;
        var text = $"{company} {url} {email}".ToLowerInvariant();
        return BlacklistedCompanies.Any(b => !string.IsNullOrWhiteSpace(b) && text.Contains(b.Trim().ToLowerInvariant()));
    }

    public int DailyRunHour { get; set; } = 9; // 9:00 AM daily
    public int MinMatchScoreToNotify { get; set; } = 60;
    public bool AutoSendEmailApplication { get; set; } = false; // Safe default; user toggles on
    public SmtpSettings Smtp { get; set; } = new();
    public TelegramSettings Telegram { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string SenderEmail { get; set; } = "sajidkhanbarun@gmail.com";
    public string SenderName { get; set; } = "Sajid Tavrej Khan";
    public string Password { get; set; } = ""; // App password
    public bool EnableSsl { get; set; } = true;
}

public class TelegramSettings
{
    public bool Enabled { get; set; } = false;
    public string BotToken { get; set; } = "";
    public string ChatId { get; set; } = "";
}
