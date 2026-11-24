using MailKit.Net.Smtp;
using MimeKit;
using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Services;

public class EmailApplicationService
{
    private readonly ILogger<EmailApplicationService> _logger;

    public EmailApplicationService(ILogger<EmailApplicationService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool success, string message)> SendApplicationEmailAsync(
        JobPosting job, 
        UserProfile profile, 
        AppConfig config, 
        string coverLetter)
    {
        // 1. Air-tight Blacklist Employer Guard
        if (config.IsBlacklisted(job.Company, job.JobUrl, job.ApplyEmail))
        {
            _logger.LogError("SECURITY BLOCK: Refusing to send application to blacklisted employer: {Company} (URL: {Url}, Email: {Email})",
                job.Company, job.JobUrl, job.ApplyEmail);
            return (false, $"BLOCKED: {job.Company} is on your blacklisted employers list (RevClerx, MaxVal, Mindmatrix, Arerhos Ventures). Application strictly rejected.");
        }

        if (string.IsNullOrWhiteSpace(job.ApplyEmail))
        {
            return (false, "No application email specified for this job.");
        }

        // If SMTP password is not configured yet, record simulation mode
        if (string.IsNullOrWhiteSpace(config.Smtp.Password))
        {
            _logger.LogInformation(
                "[SIMULATION MODE] Would send email to: {Email} for position: {Title} at {Company}. " +
                "To enable real outgoing emails, configure SMTP password in Settings.",
                job.ApplyEmail, job.Title, job.Company);

            return (true, $"[SIMULATED APPLICATION] Ready to dispatch to {job.ApplyEmail}. (Configure SMTP in settings for live delivery)");
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.Smtp.SenderName, config.Smtp.SenderEmail));
            message.To.Add(new MailboxAddress(job.Company, job.ApplyEmail));
            message.Subject = $"Application: {job.Title} - {profile.FullName} (Senior .NET & SQL Developer)";

            var builder = new BodyBuilder
            {
                TextBody = coverLetter
            };

            // Attach resume if file exists (with wwwroot fallback)
            string resumeToAttach = profile.ResumePath;
            if (string.IsNullOrWhiteSpace(resumeToAttach) || !File.Exists(resumeToAttach))
            {
                var fallback = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resume.pdf");
                if (File.Exists(fallback)) resumeToAttach = fallback;
            }

            if (!string.IsNullOrWhiteSpace(resumeToAttach) && File.Exists(resumeToAttach))
            {
                builder.Attachments.Add(resumeToAttach);
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(config.Smtp.Host, config.Smtp.Port, config.Smtp.EnableSsl);
            await client.AuthenticateAsync(config.Smtp.SenderEmail, config.Smtp.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Successfully sent application email to {Email} for {Title}", job.ApplyEmail, job.Title);
            return (true, $"Application successfully emailed to {job.ApplyEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", job.ApplyEmail);
            return (false, $"Failed to send email: {ex.Message}");
        }
    }
}
