using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using visualizer.service.Repositories;
using visualizer.service.Repositories.Dummies;

namespace visualizer.service;

public static class ConfigureService
{
    public static void Configure(IServiceCollection services)
    {
        services.AddScoped<HomeState>();
        
        services.AddSingleton<MetricsConfig>();
        var useDummy = Environment.GetEnvironmentVariable("USE_DUMMY") == "true";
        if (useDummy)
        {
            services.AddSingleton<IMetricsHandler, DummyMetricsHandler>();
            services.AddSingleton<IUserRepository, DummyUserRepository>();
        }
        else
        {
            services.AddSingleton<IMetricsHandler>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("Metrics");

                return new MetricsHandler(connectionString!);
            });

            services.AddSingleton<IUserRepository>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var connString = config.GetConnectionString("User");
                return new UserRepository(connString ?? throw new ArgumentNullException(nameof(connString)));
            });
        }
        
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(MetricsConfig.ServiceName, serviceVersion: MetricsConfig.ServiceVersion);

        services.AddOpenTelemetry().WithMetrics(metrics =>
        {
            metrics.SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    MetricsConfig.ServiceName
                ).AddPrometheusExporter();
        });
    }
}
