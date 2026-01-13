using FluentValidation;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationMessages
{
    public record StartRegistration(
        Guid OrganizationId,
        string Name,
        string Slug,
        string CreatorUserId,
        string? EmailDomain);

    public record RegistrationResult(
        Guid OrganizationId,
        bool IsSuccess,
        string? Reason);
}

public class StartRegistrationValidator : AbstractValidator<OrganizationMessages.StartRegistration>
{
    public StartRegistrationValidator()
    {
        _ = RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required")
            .MinimumLength(3).WithMessage("Organization name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Organization name cannot exceed 100 characters");

        _ = RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MinimumLength(3).WithMessage("Slug must be at least 3 characters")
            .MaximumLength(50).WithMessage("Slug cannot exceed 50 characters")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");

        _ = RuleFor(x => x.CreatorUserId)
            .NotEmpty().WithMessage("Creator user ID is required");

        _ = When(x => !string.IsNullOrWhiteSpace(x.EmailDomain), () =>
        {
            _ = RuleFor(x => x.EmailDomain)
                .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9-]{1,61}[a-zA-Z0-9]\.[a-zA-Z]{2,}$")
                .WithMessage("Email domain must be a valid domain name (e.g., example.com)");
        });
    }
}
