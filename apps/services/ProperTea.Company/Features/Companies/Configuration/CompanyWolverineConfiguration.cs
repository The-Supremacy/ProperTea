using ProperTea.Company.Extensions;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Configuration;

public static class CompanyMessagingConfiguration
{
    public static void ConfigureCompanyIntegrationEvents(this WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<CompanyIntegrationEvents.CompanyCreated>(
            "company.events");

        opts.PublishIntegrationEvent<CompanyIntegrationEvents.CompanyDeleted>(
            "company.events");
    }
}
