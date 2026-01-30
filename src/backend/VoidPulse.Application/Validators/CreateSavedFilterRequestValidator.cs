using System.Text.Json;
using FluentValidation;
using VoidPulse.Application.DTOs.SavedFilters;

namespace VoidPulse.Application.Validators;

public class CreateSavedFilterRequestValidator : AbstractValidator<CreateSavedFilterRequest>
{
    public CreateSavedFilterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.FilterJson)
            .NotEmpty().WithMessage("Filter JSON is required.")
            .Must(BeValidJson).WithMessage("Filter JSON must be valid JSON.");
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
