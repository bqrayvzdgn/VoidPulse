using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data.Configurations;

public class HttpMetadataConfiguration : IEntityTypeConfiguration<HttpMetadata>
{
    public void Configure(EntityTypeBuilder<HttpMetadata> builder)
    {
        builder.HasKey(hm => hm.Id);

        builder.Property(hm => hm.Method)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(hm => hm.Host)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(hm => hm.Path)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(hm => hm.StatusCode)
            .IsRequired();

        builder.Property(hm => hm.UserAgent)
            .HasMaxLength(1000);

        builder.Property(hm => hm.ContentType)
            .HasMaxLength(200);

        builder.Property(hm => hm.ResponseTimeMs)
            .IsRequired();

        builder.HasIndex(hm => hm.TrafficFlowId)
            .IsUnique();

        builder.HasOne(hm => hm.TrafficFlow)
            .WithOne(tf => tf.HttpMetadata)
            .HasForeignKey<HttpMetadata>(hm => hm.TrafficFlowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
