using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.Core.Tenancy;

namespace ProperTea.Infrastructure.Tenancy;

public static class Extensions
{
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services)
    {
        services.AddScoped<ICurrentOrganizationProvider, CurrentOrganizationProvider>();
        return services;
    }

    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantMiddleware>();
    }
}