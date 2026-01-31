using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data.Configurations;

public class DnsResolutionConfiguration : IEntityTypeConfiguration<DnsResolution>
{
    public void Configure(EntityTypeBuilder<DnsResolution> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.QueriedHostname)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(d => d.ResolvedIp)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(d => d.QueryType)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.ClientIp)
            .HasMaxLength(45);

        builder.Property(d => d.ResolvedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(d => new { d.TenantId, d.ResolvedAt });
        builder.HasIndex(d => new { d.TenantId, d.QueriedHostname });
        builder.HasIndex(d => new { d.TenantId, d.ResolvedIp });

        // Foreign key
        builder.HasOne(d => d.Tenant)
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
