using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data.Configurations;

public class TrafficFlowConfiguration : IEntityTypeConfiguration<TrafficFlow>
{
    public void Configure(EntityTypeBuilder<TrafficFlow> builder)
    {
        builder.HasKey(tf => tf.Id);

        builder.Property(tf => tf.SourceIp)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(tf => tf.DestinationIp)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(tf => tf.SourcePort)
            .IsRequired();

        builder.Property(tf => tf.DestinationPort)
            .IsRequired();

        builder.Property(tf => tf.Protocol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(tf => tf.BytesSent)
            .IsRequired();

        builder.Property(tf => tf.BytesReceived)
            .IsRequired();

        builder.Property(tf => tf.PacketsSent)
            .IsRequired();

        builder.Property(tf => tf.PacketsReceived)
            .IsRequired();

        builder.Property(tf => tf.StartedAt)
            .IsRequired();

        builder.Property(tf => tf.EndedAt)
            .IsRequired();

        builder.Property(tf => tf.FlowDuration)
            .IsRequired();

        builder.HasIndex(tf => tf.TenantId);

        builder.HasIndex(tf => new { tf.SourceIp, tf.DestinationIp });

        builder.HasIndex(tf => new { tf.StartedAt, tf.EndedAt });

        builder.HasIndex(tf => tf.Protocol);

        builder.HasOne(tf => tf.Tenant)
            .WithMany(t => t.TrafficFlows)
            .HasForeignKey(tf => tf.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tf => tf.AgentKey)
            .WithMany(ak => ak.TrafficFlows)
            .HasForeignKey(tf => tf.AgentKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tf => tf.HttpMetadata)
            .WithOne(hm => hm.TrafficFlow)
            .HasForeignKey<HttpMetadata>(hm => hm.TrafficFlowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
