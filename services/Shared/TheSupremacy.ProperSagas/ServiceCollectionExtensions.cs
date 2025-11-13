using Microsoft.Extensions.DependencyInjection;
using TheSupremacy.ProperSagas.Orchestration;
using TheSupremacy.ProperSagas.Services;

namespace TheSupremacy.ProperSagas;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProperSagas(
        this IServiceCollection services,
        Action<SagaRegistryBuilder> configure)
    {
        var registry = new SagaRegistry();
        var builder = new SagaRegistryBuilder(services, registry);

        configure(builder);

        services.AddSingleton(registry);
        services.AddScoped<ISagaResumeService, SagaResumeService>();
        services.AddSingleton<SagaBackgroundProcessor>();

        return services;
    }
}

public class SagaRegistryBuilder(IServiceCollection services, SagaRegistry registry)
{
    public SagaRegistryBuilder AddSaga<TOrchestrator>(string sagaType)
        where TOrchestrator : SagaOrchestratorBase
    {
        registry.Register<TOrchestrator>(sagaType);
        services.AddScoped<TOrchestrator>();

        return this;
    }
}