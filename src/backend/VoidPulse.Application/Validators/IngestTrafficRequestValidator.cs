using System.Net;
using FluentValidation;
using VoidPulse.Application.DTOs.Traffic;

namespace VoidPulse.Application.Validators;

public class IngestTrafficRequestValidator : AbstractValidator<IngestTrafficRequest>
{
    private static readonly HashSet<string> ValidProtocols = new(StringComparer.OrdinalIgnoreCase)
    {
        "TCP", "UDP", "ICMP", "ICMPv6", "SCTP", "GRE", "ESP", "AH", "IGMP"
    };

    public IngestTrafficRequestValidator()
    {
        RuleFor(x => x.SourceIp)
            .NotEmpty().WithMessage("Source IP is required.")
            .Must(BeValidIpAddress).WithMessage("Source IP must be a valid IPv4 or IPv6 address.");

        RuleFor(x => x.DestinationIp)
            .NotEmpty().WithMessage("Destination IP is required.")
            .Must(BeValidIpAddress).WithMessage("Destination IP must be a valid IPv4 or IPv6 address.");

        RuleFor(x => x.SourcePort)
            .InclusiveBetween(0, 65535).WithMessage("Source port must be between 0 and 65535.");

        RuleFor(x => x.DestinationPort)
            .InclusiveBetween(0, 65535).WithMessage("Destination port must be between 0 and 65535.");

        RuleFor(x => x.Protocol)
            .NotEmpty().WithMessage("Protocol is required.")
            .Must(p => ValidProtocols.Contains(p))
            .WithMessage($"Protocol must be one of: {string.Join(", ", ValidProtocols)}.");

        RuleFor(x => x.BytesSent)
            .GreaterThanOrEqualTo(0).WithMessage("Bytes sent must be greater than or equal to 0.")
            .LessThanOrEqualTo(10_000_000_000L).WithMessage("Bytes sent exceeds maximum allowed value.");

        RuleFor(x => x.BytesReceived)
            .GreaterThanOrEqualTo(0).WithMessage("Bytes received must be greater than or equal to 0.")
            .LessThanOrEqualTo(10_000_000_000L).WithMessage("Bytes received exceeds maximum allowed value.");

        RuleFor(x => x.PacketsSent)
            .GreaterThanOrEqualTo(0).WithMessage("Packets sent must be greater than or equal to 0.");

        RuleFor(x => x.PacketsReceived)
            .GreaterThanOrEqualTo(0).WithMessage("Packets received must be greater than or equal to 0.");

        RuleFor(x => x.StartedAt)
            .LessThan(x => x.EndedAt).WithMessage("StartedAt must be before EndedAt.");
    }

    private static bool BeValidIpAddress(string ip) =>
        !string.IsNullOrWhiteSpace(ip) && IPAddress.TryParse(ip, out _);
}
