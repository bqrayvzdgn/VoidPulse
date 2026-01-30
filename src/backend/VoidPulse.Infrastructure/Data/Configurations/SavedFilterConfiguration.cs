using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data.Configurations;

public class SavedFilterConfiguration : IEntityTypeConfiguration<SavedFilter>
{
    public void Configure(EntityTypeBuilder<SavedFilter> builder)
    {
        builder.HasKey(sf => sf.Id);

        builder.Property(sf => sf.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sf => sf.FilterJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.HasOne(sf => sf.User)
            .WithMany(u => u.SavedFilters)
            .HasForeignKey(sf => sf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sf => sf.Tenant)
            .WithMany(t => t.SavedFilters)
            .HasForeignKey(sf => sf.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
