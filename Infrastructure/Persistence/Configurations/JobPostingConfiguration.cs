using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Infrastructure.Persistence.Configurations;

public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Title).IsRequired().HasMaxLength(250);
        builder.Property(j => j.Company).IsRequired().HasMaxLength(200);
        builder.Property(j => j.JobUrl).IsRequired().HasMaxLength(1000);
        builder.Property(j => j.Country).HasMaxLength(100);
        builder.Property(j => j.Status).HasMaxLength(50).HasDefaultValue("Discovered");

        builder.HasIndex(j => j.JobUrl).IsUnique();
        builder.HasIndex(j => j.MatchScore);
        builder.HasIndex(j => j.Country);
        builder.HasIndex(j => j.Status);
    }
}
