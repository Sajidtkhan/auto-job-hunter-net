namespace JobAutoApplyNET.Domain.Enums;

public enum JobApplicationStatus
{
    Discovered,
    Applied,
    Interview,
    Rejected,
    Blacklisted
}

public enum MatchTier
{
    Low,
    Medium,
    High,
    TopMatch
}
