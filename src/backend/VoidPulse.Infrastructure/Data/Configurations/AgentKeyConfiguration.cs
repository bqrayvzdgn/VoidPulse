using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data.Configurations;

public class AgentKeyConfiguration : IEntityTypeConfiguration<AgentKey>
{
    public void Configure(EntityTypeBuilder<AgentKey> builder)
    {
        builder.HasKey(ak => ak.Id);

        builder.Property(ak => ak.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ak => ak.ApiKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(ak => ak.ApiKey)
            .IsUnique();

        builder.Property(ak => ak.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(ak => ak.Tenant)
            .WithMany(t => t.AgentKeys)
            .HasForeignKey(ak => ak.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
