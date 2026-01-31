using FluentValidation;
using VoidPulse.Application.DTOs.Retention;

namespace VoidPulse.Application.Validators;

public class RetentionPolicyRequestValidator : AbstractValidator<RetentionPolicyRequest>
{
    public RetentionPolicyRequestValidator()
    {
        RuleFor(x => x.RetentionDays)
            .InclusiveBetween(1, 3650)
            .WithMessage("Retention days must be between 1 and 3650 (10 years).");
    }
}
