namespace JobAutoApplyNET.Models;

public class JobPosting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty; // "Germany", "Dubai / UAE", "Remote"
    public string Source { get; set; } = string.Empty;  // "Arbeitnow", "Adzuna", "Jooble", "Naukrigulf", etc.
    public string JobUrl { get; set; } = string.Empty;
    public string? ApplyEmail { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Salary { get; set; }
    public DateTime PostedDate { get; set; } = DateTime.UtcNow;
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public int MatchScore { get; set; } = 0; // 0 - 100
    public string MatchedKeywords { get; set; } = string.Empty;
    public string Status { get; set; } = "Discovered"; // Discovered, Applied, Skipped, Interview, Rejected
    public DateTime? AppliedAt { get; set; }
    public string? Notes { get; set; }
    public string? GeneratedCoverLetter { get; set; }
}
