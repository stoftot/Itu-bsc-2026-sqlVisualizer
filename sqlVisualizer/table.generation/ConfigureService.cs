using animationGeneration.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace tableGeneration;

public static class ConfigureService
{
    public static void Configure(IServiceCollection services)
    {
        services.AddScoped<SQLExecutorWrapper>();
        services.AddScoped<ITablesPerExecutionStepGenerator, TablesPerExecutionStepGenerator>();
    }
}
