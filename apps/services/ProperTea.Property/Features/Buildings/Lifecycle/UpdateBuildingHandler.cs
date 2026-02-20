using Marten;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record UpdateBuilding(Guid BuildingId, string? Code, string? Name, AddressRequest? Address);

public class UpdateBuildingHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateBuilding command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var building = await session.Events.AggregateStreamAsync<BuildingAggregate>(command.BuildingId)
            ?? throw new NotFoundException(
                BuildingErrorCodes.BUILDING_NOT_FOUND,
                nameof(BuildingAggregate),
                command.BuildingId);

        var events = new List<object>();

        if (!string.IsNullOrWhiteSpace(command.Code) && building.Code != command.Code)
        {
            var codeExists = await session.Query<BuildingAggregate>()
                .Where(b => b.PropertyId == building.PropertyId
                    && b.Code == command.Code
                    && b.CurrentStatus == BuildingAggregate.Status.Active
                    && b.Id != command.BuildingId)
                .AnyAsync();

            if (codeExists)
                throw new ConflictException(
                    BuildingErrorCodes.BUILDING_CODE_ALREADY_EXISTS,
                    $"A building with code '{command.Code}' already exists in this property");

            events.Add(building.UpdateCode(command.Code));
        }

        if (!string.IsNullOrWhiteSpace(command.Name) && building.Name != command.Name)
        {
            events.Add(building.UpdateName(command.Name));
        }

        if (command.Address != null)
        {
            var newAddress = command.Address.ToAddress();
            if (newAddress != building.Address)
                events.Add(building.UpdateAddress(newAddress));
        }

        if (events.Count > 0)
        {
            _ = session.Events.Append(command.BuildingId, [.. events]);
            await session.SaveChangesAsync();

            var effectiveAddress = command.Address?.ToAddress() ?? building.Address;
            var organizationId = session.TenantId;
            await bus.PublishAsync(new BuildingIntegrationEvents.BuildingUpdated
            {
                BuildingId = command.BuildingId,
                PropertyId = building.PropertyId,
                OrganizationId = organizationId,
                Code = command.Code ?? building.Code,
                Name = command.Name ?? building.Name,
                Address = new Contracts.Events.AddressData(
                    effectiveAddress.Country.ToString(),
                    effectiveAddress.City,
                    effectiveAddress.ZipCode,
                    effectiveAddress.StreetAddress),
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}
