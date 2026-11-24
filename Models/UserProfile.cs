namespace JobAutoApplyNET.Models;

public class UserProfile
{
    public string FullName { get; set; } = "Sajid Tavrej Khan";
    public string Email { get; set; } = "sajidkhanbarun@gmail.com";
    public string Phone { get; set; } = "+91-6280525099";
    public string LinkedInUrl { get; set; } = "https://in.linkedin.com/in/sajid-tavrej-khan-b1b0b6188";
    public string CurrentTitle { get; set; } = "Senior .NET Core Backend Developer";
    public int YearsOfExperience { get; set; } = 6;
    public string NoticePeriod { get; set; } = "30 Days (Immediate Available)";
    public bool WillingToRelocate { get; set; } = true;
    public string TargetLocations { get; set; } = "Dubai & Abu Dhabi (UAE); Riyadh & Jeddah (Saudi Arabia); Berlin, Munich & Frankfurt (Germany); Remote";
    public string SkillsSummary { get; set; } = "C#, .NET 8 / .NET Core, ASP.NET Core Web API, Entity Framework Core, Dapper, MS SQL Server, Clean Architecture, Microservices, Rest API, jQuery, MVC";
    public string ResumePath { get; set; } = @"C:\Users\Sajid Khan\Downloads\Sajid_Tavrej_Khan_Resume.pdf";
}
