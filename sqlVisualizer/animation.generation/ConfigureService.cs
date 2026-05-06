using Microsoft.Extensions.DependencyInjection;
using visualizer.service.Contracts;

namespace animationGeneration;

public static class ConfigureService
{
    public static void Configure(IServiceCollection services)
    {
        services.AddScoped<IAnimationGenerator, AnimationGenerator>();
        services.AddScoped<IDisplayTableGenerator, Models.DisplayTableGenerator>();
    }
}
