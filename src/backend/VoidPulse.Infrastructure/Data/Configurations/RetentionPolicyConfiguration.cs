using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data.Configurations;

public class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.RetentionDays)
            .HasDefaultValue(90);

        builder.HasIndex(rp => rp.TenantId)
            .IsUnique();

        builder.HasOne(rp => rp.Tenant)
            .WithOne(t => t.RetentionPolicy)
            .HasForeignKey<RetentionPolicy>(rp => rp.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
