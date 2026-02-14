using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record UpdateCompany(Guid CompanyId, string? Code, string? Name);

public class UpdateCompanyHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateCompany command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var company = await session.Events.AggregateStreamAsync<CompanyAggregate>(command.CompanyId)
            ?? throw new NotFoundException(
                CompanyErrorCodes.COMPANY_NOT_FOUND,
                "Company",
                command.CompanyId);

        var events = new List<object>();

        if (!string.IsNullOrWhiteSpace(command.Code) && company.Code != command.Code)
        {
            // Validate code uniqueness within tenant
            var codeExists = await session.Query<CompanyAggregate>()
                .Where(c => c.Code == command.Code
                    && c.CurrentStatus == CompanyAggregate.Status.Active
                    && c.Id != command.CompanyId)
                .AnyAsync();

            if (codeExists)
                throw new ConflictException(
                    CompanyErrorCodes.COMPANY_CODE_ALREADY_EXISTS,
                    $"A company with code '{command.Code}' already exists");

            events.Add(company.UpdateCode(command.Code));
        }

        if (!string.IsNullOrWhiteSpace(command.Name) && company.Name != command.Name)
        {
            // Validate name uniqueness within tenant
            var nameExists = await session.Query<CompanyAggregate>()
                .Where(c => c.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase)
                    && c.CurrentStatus == CompanyAggregate.Status.Active
                    && c.Id != command.CompanyId)
                .AnyAsync();

            if (nameExists)
                throw new ConflictException(
                    CompanyErrorCodes.COMPANY_NAME_ALREADY_EXISTS,
                    $"A company with name '{command.Name}' already exists");

            events.Add(company.UpdateName(command.Name));
        }

        if (events.Count > 0)
        {
            _ = session.Events.Append(command.CompanyId, [.. events]);
            await session.SaveChangesAsync();

            await bus.PublishAsync(new CompanyIntegrationEvents.CompanyUpdated
            {
                CompanyId = command.CompanyId,
                OrganizationId = session.TenantId,
                Code = command.Code ?? company.Code,
                Name = command.Name ?? company.Name,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}
