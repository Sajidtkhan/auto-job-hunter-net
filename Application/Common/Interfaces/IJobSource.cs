using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Application.Common.Interfaces;

public interface IJobSource
{
    string SourceName { get; }
    Task<List<JobPosting>> FetchJobsAsync(CancellationToken cancellationToken = default);
}
