using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TheSupremacy.ProperCqrs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProperCqrs(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<ICommandBus, CommandBus>();
        services.AddScoped<IQueryBus, QueryBus>();

        services.AddValidatorsFromAssemblies(assemblies);

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Decorate(typeof(ICommandHandler<>), (handler, sp) =>
            DecoratorHelper.DecorateWithValidation(
                handler, sp,
                typeof(ICommandHandler<>),
                typeof(ValidationCommandHandlerDecorator<>)));

        services.Decorate(typeof(ICommandHandler<,>), (handler, sp) =>
            DecoratorHelper.DecorateWithValidation(
                handler, sp,
                typeof(ICommandHandler<,>),
                typeof(ValidationCommandHandlerDecorator<,>)));

        services.Decorate(typeof(IQueryHandler<,>), (handler, sp) =>
            DecoratorHelper.DecorateWithValidation(
                handler, sp,
                typeof(IQueryHandler<,>),
                typeof(ValidationQueryHandlerDecorator<,>)));

        return services;
    }
}