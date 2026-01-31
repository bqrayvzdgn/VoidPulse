using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data.Configurations;

public class CapturedPacketConfiguration : IEntityTypeConfiguration<CapturedPacket>
{
    public void Configure(EntityTypeBuilder<CapturedPacket> builder)
    {
        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.CapturedAt)
            .IsRequired();

        builder.Property(cp => cp.SourceIp)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(cp => cp.DestinationIp)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(cp => cp.SourcePort)
            .IsRequired();

        builder.Property(cp => cp.DestinationPort)
            .IsRequired();

        builder.Property(cp => cp.Protocol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(cp => cp.PacketLength)
            .IsRequired();

        builder.Property(cp => cp.HeaderBytes)
            .IsRequired();

        builder.Property(cp => cp.ProtocolStack)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(cp => cp.Info)
            .IsRequired()
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(cp => new { cp.TenantId, cp.CapturedAt });
        builder.HasIndex(cp => cp.TrafficFlowId);
        builder.HasIndex(cp => new { cp.TenantId, cp.SourceIp, cp.DestinationIp });

        // Relationships
        builder.HasOne(cp => cp.Tenant)
            .WithMany()
            .HasForeignKey(cp => cp.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cp => cp.TrafficFlow)
            .WithMany(tf => tf.CapturedPackets)
            .HasForeignKey(cp => cp.TrafficFlowId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
