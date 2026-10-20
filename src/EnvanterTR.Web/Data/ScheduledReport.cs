using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnvanterTR.Web.Data;

public sealed class ScheduledReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string ReportKind { get; set; }
    public string Frequency { get; set; } = "weekly";
    public string TimeZoneId { get; set; } = "Europe/Istanbul";
    public int LocalHour { get; set; } = 9;
    public int LocalMinute { get; set; }
    public int WeekDay { get; set; } = 1;
    public int MonthDay { get; set; } = 1;
    public string Language { get; set; } = "tr";
    public bool IsEnabled { get; set; }
    public DateTimeOffset NextDueAt { get; set; }
    public DateTimeOffset? LastSentAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? RetryAfter { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public Guid? ClaimToken { get; set; }
    public int AttemptCount { get; set; }
    public string LastStatus { get; set; } = "Idle";
    public string? LastErrorCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid Version { get; set; } = Guid.NewGuid();
}

internal sealed class ScheduledReportConfiguration : IEntityTypeConfiguration<ScheduledReport>
{
    public void Configure(EntityTypeBuilder<ScheduledReport> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ReportKind).HasMaxLength(24).IsRequired();
        builder.Property(x => x.Frequency).HasMaxLength(12).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(2).IsRequired();
        builder.Property(x => x.LastStatus).HasMaxLength(16).IsRequired();
        builder.Property(x => x.LastErrorCode).HasMaxLength(64);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.CompanyId, x.UserId });
        builder.HasIndex(x => new { x.IsEnabled, x.NextDueAt });
        builder.HasOne<CompanyMembership>().WithMany().HasForeignKey(x => new { x.CompanyId, x.UserId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_ScheduledReports_Kind", "\"ReportKind\" IN ('inventory', 'maintenance', 'licenses', 'stock')");
            table.HasCheckConstraint("CK_ScheduledReports_Frequency", "\"Frequency\" IN ('daily', 'weekly', 'monthly')");
            table.HasCheckConstraint("CK_ScheduledReports_Time", "\"LocalHour\" BETWEEN 0 AND 23 AND \"LocalMinute\" BETWEEN 0 AND 59 AND \"WeekDay\" BETWEEN 0 AND 6 AND \"MonthDay\" BETWEEN 1 AND 28");
            table.HasCheckConstraint("CK_ScheduledReports_Language", "\"Language\" IN ('tr', 'en', 'ru', 'uk', 'mk')");
            table.HasCheckConstraint("CK_ScheduledReports_Status", "\"LastStatus\" IN ('Idle', 'Sending', 'Sent', 'Failed', 'Skipped') AND \"AttemptCount\" >= 0");
        });
    }
}
