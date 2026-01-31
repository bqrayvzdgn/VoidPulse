using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Message).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Severity).IsRequired();
        builder.Property(a => a.SourceIp).HasMaxLength(45);
        builder.Property(a => a.DestinationIp).HasMaxLength(45);
        builder.Property(a => a.MetadataJson).HasMaxLength(2000);
        builder.Property(a => a.TriggeredAt).IsRequired();

        builder.HasIndex(a => new { a.TenantId, a.TriggeredAt });
        builder.HasIndex(a => new { a.TenantId, a.IsAcknowledged });

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AlertRule)
            .WithMany(r => r.Alerts)
            .HasForeignKey(a => a.AlertRuleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
