using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record UpdateBuilding(Guid BuildingId, string? Code, string? Name);

public class UpdateBuildingHandler : IWolverineHandler
{
    public async Task Handle(UpdateBuilding command, IDocumentSession session)
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

        if (events.Count > 0)
        {
            _ = session.Events.Append(command.BuildingId, [.. events]);
        }
    }
}