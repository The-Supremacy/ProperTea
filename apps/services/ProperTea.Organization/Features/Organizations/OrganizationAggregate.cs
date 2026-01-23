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
    public string? ExternalOrganizationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    #region Factory Methods
    public static Created Create(Guid id, string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(nameof(name), "Name is required");

        ValidateSlug(slug);

        return new Created(id, name, slug, DateTimeOffset.UtcNow);
    }

    public static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new BusinessViolationException(nameof(slug), "Slug is required");

        if (slug.Length is < MinSlugLength or > MaxSlugLength)
            throw new BusinessViolationException(
                nameof(slug),
                $"Slug must be between {MinSlugLength} and {MaxSlugLength} characters");

        if (!SlugPattern().IsMatch(slug))
            throw new BusinessViolationException(
                nameof(slug),
                "Slug must contain only lowercase letters, numbers, and hyphens.");
    }

    public static ExternalOrganizationCreated LinkExternalOrganization(Guid organizationId, string externalOrganizationId)
    {
        if (string.IsNullOrWhiteSpace(externalOrganizationId))
            throw new BusinessViolationException(nameof(externalOrganizationId), "External Organization ID is required");

        return new ExternalOrganizationCreated(organizationId, externalOrganizationId);
    }

    public Activated Activate()
    {
        return new Activated(Id, DateTimeOffset.UtcNow);
    }

    public NameChanged Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new BusinessViolationException(nameof(newName), "Name is required");
        if (newName == Name)
            throw new BusinessViolationException("New name is the same as current name");
        return new NameChanged(Id, newName, DateTimeOffset.UtcNow);
    }

    public SlugChanged ChangeSlug(string newSlug)
    {
        ValidateSlug(newSlug);
        if (newSlug == Slug) throw new BusinessViolationException("New slug is the same as current slug");
        return new SlugChanged(Id, newSlug, DateTimeOffset.UtcNow);
    }

    public Deactivated Deactivate(string reason)
    {
        if (CurrentStatus == Status.Deactivated)
            throw new BusinessViolationException("Organization is already deactivated");
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessViolationException(nameof(reason), "Reason is required");
        return new Deactivated(Id, reason, DateTimeOffset.UtcNow);
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

    public void Apply(ExternalOrganizationCreated e)
    {
        ExternalOrganizationId = e.ExternalOrganizationId;
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
