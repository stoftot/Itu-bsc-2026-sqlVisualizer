using Microsoft.Extensions.DependencyInjection;

namespace commonDataModels;

public static class ConfigureService
{
    public static void Configure(IServiceCollection services)
    {
        services.AddScoped<Models.ICurrentDatabaseContext, Models.CurrentDatabaseContext>();
    }
}
