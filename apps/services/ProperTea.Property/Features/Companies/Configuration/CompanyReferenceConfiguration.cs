using Marten;

namespace ProperTea.Property.Features.Companies.Configuration;

public static class CompanyReferenceConfiguration
{
    public static void ConfigureCompanyReferenceMarten(this StoreOptions opts)
    {
        _ = opts.Schema.For<CompanyReference>()
            .Index(x => x.Code)
            .Index(x => x.Name)
            .Index(x => x.IsDeleted);
    }

    public static IServiceCollection AddCompanyReferenceHttpClient(this IServiceCollection services)
    {
        _ = services.AddHttpClient("company", client =>
        {
            client.BaseAddress = new Uri("https+http://company");
        });

        return services;
    }
}
