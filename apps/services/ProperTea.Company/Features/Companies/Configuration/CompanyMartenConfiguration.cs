using Marten;
using Marten.Events.Projections;

namespace ProperTea.Company.Features.Companies.Configuration;

public static class CompanyMartenConfiguration
{
    public static void ConfigureCompanyMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<CompanyAggregate>(SnapshotLifecycle.Inline);

        _ = opts.Schema.For<CompanyAggregate>()
            .UniqueIndex(x => x.Code)
            .Index(x => x.Name)
            .Index(x => x.CurrentStatus);

        // Convention: {aggregate}.{event-name}.v{version}
        opts.Events.MapEventType<CompanyEvents.Created>("company.created.v1");
        opts.Events.MapEventType<CompanyEvents.CodeUpdated>("company.code-updated.v1");
        opts.Events.MapEventType<CompanyEvents.NameUpdated>("company.name-updated.v1");
        opts.Events.MapEventType<CompanyEvents.Deleted>("company.deleted.v1");
    }
}
