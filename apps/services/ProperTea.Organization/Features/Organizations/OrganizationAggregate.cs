using System.Text.RegularExpressions;
using Marten.Metadata;
using ProperTea.ServiceDefaults.Exceptions;
using static ProperTea.Organization.Features.Organizations.OrganizationEvents;

namespace ProperTea.Organization.Features.Organizations;

public partial class OrganizationAggregate : IRevisioned
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex SlugPattern();

    private const int MinSlugLength = 2;
    private const int MaxSlugLength = 50;

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Status CurrentStatus { get; set; }
    public string? ZitadelOrganizationId { get; set; }
    public string? EmailDomain { get; set; }
    public bool IsDomainVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    #region Factory Methods
    public static Created Create(Guid id, string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(nameof(name), "Name is required");

        ValidateSlug(slug);

        return new Created(id, name, slug, DateTimeOffset.UtcNow);
    }

    public static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ValidationException(nameof(slug), "Slug is required");

        if (slug.Length is < MinSlugLength or > MaxSlugLength)
            throw new ValidationException(
                nameof(slug),
                $"Slug must be between {MinSlugLength} and {MaxSlugLength} characters");

        if (!SlugPattern().IsMatch(slug))
            throw new ValidationException(
                nameof(slug),
                "Slug must contain only lowercase letters, numbers, and hyphens. " +
                "Cannot start or end with a hyphen, or have consecutive hyphens.");
    }

    public static ZitadelOrganizationCreated LinkZitadel(Guid organizationId, string zitadelOrganizationId)
    {
        if (string.IsNullOrWhiteSpace(zitadelOrganizationId))
            throw new ValidationException(nameof(zitadelOrganizationId), "ZITADEL Organization ID is required");

        return new ZitadelOrganizationCreated(organizationId, zitadelOrganizationId);
    }

    public static Activated Activate(Guid organizationId)
    {
        return new Activated(organizationId, DateTimeOffset.UtcNow);
    }

    public NameChanged Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ValidationException(nameof(newName), "Name is required");

        if (newName == Name)
            throw new BusinessRuleViolationException("New name is the same as current name");

        return new NameChanged(Id, newName, DateTimeOffset.UtcNow);
    }

    public SlugChanged ChangeSlug(string newSlug)
    {
        ValidateSlug(newSlug);

        if (newSlug == Slug)
            throw new BusinessRuleViolationException("New slug is the same as current slug");

        return new SlugChanged(Id, newSlug, DateTimeOffset.UtcNow);
    }

    public Deactivated Deactivate(string reason)
    {
        if (CurrentStatus == Status.Deactivated)
            throw new BusinessRuleViolationException("Organization is already deactivated");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException(nameof(reason), "Reason is required");

        return new Deactivated(Id, reason, DateTimeOffset.UtcNow);
    }

    public Activated Activate()
    {
        if (CurrentStatus == Status.Active)
            throw new BusinessRuleViolationException("Organization is already active");

        return new Activated(Id, DateTimeOffset.UtcNow);
    }

    public static DomainAdded AddDomain(Guid organizationId, string emailDomain)
    {
        if (string.IsNullOrWhiteSpace(emailDomain))
            throw new ValidationException(nameof(emailDomain), "Email domain is required");

        // Basic domain validation (e.g., "example.com")
        if (!emailDomain.Contains('.') || emailDomain.StartsWith('.') || emailDomain.EndsWith('.'))
            throw new ValidationException(nameof(emailDomain), "Invalid email domain format");

        return new DomainAdded(organizationId, emailDomain, DateTimeOffset.UtcNow);
    }

    public DomainVerified VerifyDomain()
    {
        if (string.IsNullOrEmpty(EmailDomain))
            throw new BusinessRuleViolationException("No domain to verify");

        if (IsDomainVerified)
            throw new BusinessRuleViolationException("Domain is already verified");

        return new DomainVerified(Id, DateTimeOffset.UtcNow);
    }

    #endregion

    #region Event Appliers

    public void Apply(Created e)
    {
        Id = e.OrganizationId;
        Name = e.Name;
        Slug = e.Slug;
        CurrentStatus = Status.Pending;
        CreatedAt = e.CreatedAt;
    }

    public void Apply(ZitadelOrganizationCreated e)
    {
        ZitadelOrganizationId = e.ZitadelOrganizationId;
    }

    public void Apply(Activated e)
    {
        CurrentStatus = Status.Active;
    }

    public void Apply(NameChanged e)
    {
        Name = e.NewName;
    }

    public void Apply(SlugChanged e)
    {
        Slug = e.NewSlug;
    }

    public void Apply(Deactivated e)
    {
        CurrentStatus = Status.Deactivated;
    }

    public void Apply(DomainAdded e)
    {
        EmailDomain = e.EmailDomain;
        IsDomainVerified = false;
    }

    public void Apply(DomainVerified e)
    {
        IsDomainVerified = true;
    }

    #endregion

    public enum Status
    {
        Pending = 1,
        Active = 2,
        Deactivated = 3
    }

    public enum SubscriptionTier
    {
        Demo = 1,
        Trial = 2,
        Active = 3,
        Deactivated = 4
    }
}
