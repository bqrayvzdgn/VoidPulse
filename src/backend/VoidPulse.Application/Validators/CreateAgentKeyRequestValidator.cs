using FluentValidation;
using VoidPulse.Application.DTOs.Agents;

namespace VoidPulse.Application.Validators;

public class CreateAgentKeyRequestValidator : AbstractValidator<CreateAgentKeyRequest>
{
    public CreateAgentKeyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
    }
}
