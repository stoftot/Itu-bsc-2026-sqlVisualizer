using commonDataModels;
using Microsoft.Extensions.DependencyInjection;

namespace sql.executor;

public static class ConfigureService
{
    public static void Configure(IServiceCollection services)
    {
        services.AddScoped<ISQLExecutor, SQLExecutor>();
    }
}
