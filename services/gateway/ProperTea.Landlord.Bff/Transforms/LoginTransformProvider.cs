using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace ProperTea.Landlord.Bff.Transforms;

public class LoginTransformProvider : ITransformProvider
{
    private readonly IServiceProvider _serviceProvider;

    public LoginTransformProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ValidateRoute(TransformRouteValidationContext context) { }
    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        if (context.Route.Metadata?.TryGetValue("LoginTransform", out var transformValue) == true &&
            transformValue == "true")
        {
            context.AddResponseTransform(async c =>
            {
                var transform = ActivatorUtilities.CreateInstance<LoginResponseTransform>(_serviceProvider);
                await transform.ApplyAsync(c);
            });
        }
    }
}