using ProperTea.Organization.Features.Organizations.Create;
using Wolverine;

namespace ProperTea.Organization.Configuration;

public class MessagingExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        // === PUBLISHING (Outgoing Messages) ===
        options.PublishMessage<OrganizationProvisioned>().ToLocalQueue("organizations");
        options.PublishMessage<OrganizationProvisioned>().ToTopic("organizations");

        // === LISTENING (Incoming Messages) ===
    }
}
