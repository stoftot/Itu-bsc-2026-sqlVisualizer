using commonDataModels.Models;
using DuckDB.NET.Native;
using MudBlazor.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using visualizer.Components;
using visualizer.Infrastructure;
using visualizer.service;
using visualizer.service.Repositories;
using visualizer.service.Repositories.Dummies;

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
