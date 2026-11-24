using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Services;

public interface IJobSource
{
    string SourceName { get; }
    Task<List<JobPosting>> FetchJobsAsync(CancellationToken cancellationToken = default);
}
