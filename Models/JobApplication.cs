namespace JobAutoApplyNET.Models;

public class JobApplication
{
    public int Id { get; set; }
    public int JobPostingId { get; set; }
    public JobPosting? JobPosting { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = "WebClick"; // "Email", "WebClick", "AutoFill"
    public string Status { get; set; } = "Success";  // "Success", "Failed", "Pending"
    public string? Notes { get; set; }
}
