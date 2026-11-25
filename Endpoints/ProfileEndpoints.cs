using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Endpoints;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/profile")
                          .WithTags("Candidate Profile");

        group.MapGet("/", (UserProfile profile) => Results.Ok(profile))
             .WithName("GetCandidateProfile")
             .WithSummary("Retrieve candidate's active profile, core skills, and linked ATS resume path");

        group.MapPost("/", (UserProfile updated, UserProfile profile, IWebHostEnvironment env) =>
        {
            if (!string.IsNullOrWhiteSpace(updated.FullName)) profile.FullName = updated.FullName;
            if (!string.IsNullOrWhiteSpace(updated.Email)) profile.Email = updated.Email;
            if (!string.IsNullOrWhiteSpace(updated.Phone)) profile.Phone = updated.Phone;
            if (!string.IsNullOrWhiteSpace(updated.LinkedInUrl)) profile.LinkedInUrl = updated.LinkedInUrl;
            if (!string.IsNullOrWhiteSpace(updated.CurrentTitle)) profile.CurrentTitle = updated.CurrentTitle;
            if (updated.YearsOfExperience > 0) profile.YearsOfExperience = updated.YearsOfExperience;
            if (!string.IsNullOrWhiteSpace(updated.NoticePeriod)) profile.NoticePeriod = updated.NoticePeriod;
            if (!string.IsNullOrWhiteSpace(updated.TargetLocations)) profile.TargetLocations = updated.TargetLocations;
            if (!string.IsNullOrWhiteSpace(updated.SkillsSummary)) profile.SkillsSummary = updated.SkillsSummary;
            if (!string.IsNullOrWhiteSpace(updated.ResumePath)) profile.ResumePath = updated.ResumePath;

            // Persist permanently to disk
            var path = Path.Combine(env.ContentRootPath, "profile.json");
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(profile, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            return Results.Ok(profile);
        })
        .WithName("UpdateCandidateProfile")
        .WithSummary("Persist updated candidate details and core skills permanently to disk storage");

        return group;
    }
}
