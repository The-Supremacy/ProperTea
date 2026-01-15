using Marten;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record VerifyDomainCommand(Guid OrganizationId, CancellationToken Ct = default);

public class VerifyDomainHandler : IWolverineHandler
{
    public async Task Handle(
        VerifyDomainCommand command,
        IDocumentSession session,
        IZitadelClient zitadelClient,
        IMessageBus messageBus,
        ILogger logger,
        CancellationToken ct)
    {
        var org = await session.Events.AggregateStreamAsync<OrganizationAggregate>(
            command.OrganizationId,
            token: ct) ?? throw new NotFoundException(nameof(OrganizationAggregate), command.OrganizationId);
        if (org.IsDomainVerified)
        {
            throw new BusinessRuleViolationException("Domain is already verified");
        }

        if (string.IsNullOrEmpty(org.EmailDomain))
        {
            throw new BusinessRuleViolationException("No domain configured for this organization");
        }

        if (string.IsNullOrEmpty(org.ZitadelOrganizationId))
        {
            throw new BusinessRuleViolationException("Organization not linked to ZITADEL");
        }

        // Verify domain in ZITADEL (validates DNS TXT record)
        await zitadelClient.VerifyOrgDomainAsync(
            org.ZitadelOrganizationId,
            org.EmailDomain,
            ct);

        // Emit domain verified event
        var domainVerified = org.VerifyDomain();
        _ = session.Events.Append(org.Id, domainVerified);

        await session.SaveChangesAsync(ct);

        var integrationEvent = new OrganizationIntegrationEvents.OrganizationDomainVerified(
                command.OrganizationId,
                org.EmailDomain,
                DateTimeOffset.UtcNow);
        await messageBus.PublishAsync(integrationEvent);

        logger.LogInformation(
            "Verified domain for organization: {OrgId} - Domain: {Domain}",
            command.OrganizationId,
            org.EmailDomain);
    }
}
