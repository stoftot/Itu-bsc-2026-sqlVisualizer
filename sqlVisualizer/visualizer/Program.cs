using MudBlazor.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using visualizer;
using Visualizer;
using visualizer.Components;
using visualizer.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<MetricsConfig>();
var useDummy = Environment.GetEnvironmentVariable("USE_DUMMY") == "true";
if (useDummy)
{
    builder.Services.AddSingleton<IMetricsHandler, DummyMetricsHandler>();
    builder.Services.AddSingleton<IUserRepository, DummyUserRepository>();
}
else
{
    builder.Services.AddSingleton<IMetricsHandler>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("Metrics");

        return new MetricsHandler(connectionString!);
    });

    builder.Services.AddSingleton<IUserRepository>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var connString = config.GetConnectionString("User");
        return new UserRepository(connString ?? throw new ArgumentNullException(nameof(connString)));
    });
}

builder.Services.AddMudServices();
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(MetricsConfig.ServiceName, serviceVersion: MetricsConfig.ServiceVersion);

builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
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

builder.Services.AddScoped<ICurrentDatabaseContext, CurrentDatabaseContext>();
builder.Services.AddScoped<SQLExecutor>();
builder.Services.AddScoped<ISQLDecomposer, DuckDbSQLDecomposer>();
builder.Services.AddScoped<TableGenerator>();
builder.Services.AddScoped<TableOriginColumnsGenerator>();
builder.Services.AddScoped<AliasReplacer>();
builder.Services.AddScoped<VisualisationsGenerator>();
builder.Services.AddScoped<HomeState>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

//Add infastructure setup here

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
