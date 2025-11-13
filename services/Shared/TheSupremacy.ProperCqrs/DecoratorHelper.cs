using FluentValidation;

namespace TheSupremacy.ProperCqrs;

public static class DecoratorHelper
{
    public static object DecorateWithValidation(
        object handler,
        IServiceProvider serviceProvider,
        Type handlerInterfaceType,
        Type decoratorType)
    {
        var handlerInterface = handler.GetType().GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);

        var messageType = handlerInterface.GetGenericArguments()[0];
        var validatorType = typeof(IValidator<>).MakeGenericType(messageType);
        var validator = serviceProvider.GetService(validatorType);

        if (validator == null)
            return handler;

        var genericArgs = handlerInterface.GetGenericArguments();
        var concreteDecoratorType = genericArgs.Length == 1
            ? decoratorType.MakeGenericType(messageType)
            : decoratorType.MakeGenericType(messageType, genericArgs[1]);

        return Activator.CreateInstance(concreteDecoratorType, handler, validator)!;
    }
}