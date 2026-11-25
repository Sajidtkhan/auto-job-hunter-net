namespace JobAutoApplyNET.Application.DTOs;

public record JobPostingDto(
    int Id,
    string Title,
    string Company,
    string Location,
    string Country,
    string JobUrl,
    int MatchScore,
    string MatchedKeywords,
    string Status,
    string? Salary,
    DateTime DiscoveredAt,
    DateTime? AppliedAt
);

public record ScanSummaryDto(
    int TotalDiscovered,
    int NewMatchesFound,
    int DubaiCount,
    int GermanyCount,
    int AutoAppliedCount,
    DateTime CompletedAt
);
