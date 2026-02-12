using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record CreateCompany(string Code, string Name);

public class CreateCompanyHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        CreateCompany command,
        IDocumentSession session,
        IMessageBus bus)
    {
        // Validate code uniqueness within tenant
        var codeExists = await session.Query<CompanyAggregate>()
            .Where(c => c.Code == command.Code && c.CurrentStatus == CompanyAggregate.Status.Active)
            .AnyAsync();

        if (codeExists)
            throw new ConflictException(
                CompanyErrorCodes.COMPANY_CODE_ALREADY_EXISTS,
                $"A company with code '{command.Code}' already exists");

        var companyId = Guid.NewGuid();
        var created = CompanyAggregate.Create(companyId, command.Code, command.Name, DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<CompanyAggregate>(companyId, created);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new CompanyIntegrationEvents.CompanyCreated
        {
            CompanyId = companyId,
            OrganizationId = Guid.Parse(session.TenantId),
            Code = command.Code,
            Name = command.Name,
            CreatedAt = created.CreatedAt
        });

        return companyId;
    }
}
