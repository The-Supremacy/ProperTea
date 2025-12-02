using Wolverine;

namespace ProperTea.Organization.Configuration;

public class MessagingExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        // === PUBLISHING (Outgoing Messages) ===
        options.PublishMessage<Features.Organizations.OrganizationCreated>().ToLocalQueue("organizations");
        options.PublishMessage<Features.Organizations.OrganizationCreated>().ToTopic("organizations");

        // === LISTENING (Incoming Messages) ===
    }
}
