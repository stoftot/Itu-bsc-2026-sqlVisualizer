using Microsoft.Extensions.DependencyInjection;
using tableGeneration.Contracts;
using visualizer.service.Contracts;

namespace inputParsing;

public static class ConfigureService
{
    public static void Configure(IServiceCollection services)
    {
        services.AddScoped<ISQLParser, SQLParser>();
        services.AddScoped<ISQLInputValidator, SQLInputValidator>();
    }
}
