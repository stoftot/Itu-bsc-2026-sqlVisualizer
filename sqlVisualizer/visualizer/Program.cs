using DuckDB.NET.Data;
using MudBlazor.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using visualizer;
using visualizer.Components;
using visualizer.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<MetricsConfig>();
var useDummy = Environment.GetEnvironmentVariable("USE_DUMMY_METRICS") == "true";
if (useDummy)
{
    builder.Services.AddSingleton<IMetricsHandler, DummyMetricsHandler>();
}
else
{
    builder.Services.AddSingleton<IMetricsHandler>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("Metrics");

        return new MetricsHandler(connectionString!);
    });
}

builder.Services.AddSingleton<UserRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connString = config.GetConnectionString("User");
    return new UserRepository(connString ?? throw new ArgumentNullException(nameof(connString)));
});

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

builder.Services.AddScoped<DuckDBConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connString = config.GetConnectionString("Default");
    return new DuckDBConnection(connString ?? throw new ArgumentNullException(nameof(connString)));
});

builder.Services.AddScoped<SQLExecutor>();
builder.Services.AddScoped<SQLDecomposer>();
builder.Services.AddScoped<TableGenerator>();
builder.Services.AddScoped<TableOriginColumnsGenerator>();
builder.Services.AddScoped<VisualisationsGenerator>();
builder.Services.AddScoped<State>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.Use(async (context, next) =>
{
    const string cookieName = "session_id";

    if (!context.Request.Cookies.ContainsKey(cookieName))
    {
        var id = Guid.NewGuid().ToString("N");

        context.Response.Cookies.Append(cookieName, id, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddMonths(6)
        });
    }

    await next();
});

app.MapPrometheusScrapingEndpoint();

new DbInitializer(app.Configuration).Initialize();
new DbInitializer(app.Configuration).InitializeMetrics();
new DbInitializer(app.Configuration).InitializeUser();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();