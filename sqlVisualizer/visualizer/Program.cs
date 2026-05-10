using MudBlazor.Services;
using visualizer.Components;
using visualizer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

Startup.ConfigureExternalServices(builder.Services, builder.Configuration);

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

Startup.ConfigurePipeline(app);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
