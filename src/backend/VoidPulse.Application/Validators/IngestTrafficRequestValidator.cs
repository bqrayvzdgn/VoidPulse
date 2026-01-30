using FluentValidation;
using VoidPulse.Application.DTOs.Traffic;

namespace VoidPulse.Application.Validators;

public class IngestTrafficRequestValidator : AbstractValidator<IngestTrafficRequest>
{
    public IngestTrafficRequestValidator()
    {
        RuleFor(x => x.SourceIp)
            .NotEmpty().WithMessage("Source IP is required.");

        RuleFor(x => x.DestinationIp)
            .NotEmpty().WithMessage("Destination IP is required.");

        RuleFor(x => x.SourcePort)
            .InclusiveBetween(0, 65535).WithMessage("Source port must be between 0 and 65535.");

        RuleFor(x => x.DestinationPort)
            .InclusiveBetween(0, 65535).WithMessage("Destination port must be between 0 and 65535.");

        RuleFor(x => x.Protocol)
            .NotEmpty().WithMessage("Protocol is required.");

        RuleFor(x => x.BytesSent)
            .GreaterThanOrEqualTo(0).WithMessage("Bytes sent must be greater than or equal to 0.");

        RuleFor(x => x.BytesReceived)
            .GreaterThanOrEqualTo(0).WithMessage("Bytes received must be greater than or equal to 0.");

        RuleFor(x => x.PacketsSent)
            .GreaterThanOrEqualTo(0).WithMessage("Packets sent must be greater than or equal to 0.");

        RuleFor(x => x.PacketsReceived)
            .GreaterThanOrEqualTo(0).WithMessage("Packets received must be greater than or equal to 0.");

        RuleFor(x => x.StartedAt)
            .LessThan(x => x.EndedAt).WithMessage("StartedAt must be before EndedAt.");
    }
}
