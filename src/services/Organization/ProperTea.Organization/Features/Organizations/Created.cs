using Wolverine;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationCreatedHandler
{
    public static async Task HandleAsync(
        OrganizationCreated command,
        IMessageBus bus)
    {
        var a = command;

        await Task.Run(() => {}).ConfigureAwait(false);
    }
}
