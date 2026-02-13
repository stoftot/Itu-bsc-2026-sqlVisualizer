using DuckDB.NET.Data;
using visualizer;
using visualizer.Components;
using visualizer.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<DuckDBConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connString = config.GetConnectionString("Default");
    return new DuckDBConnection(connString ?? throw new ArgumentNullException(nameof(connString)));
});

builder.Services.AddScoped<SQLExecutor>();
builder.Services.AddScoped<SQLDecomposer>();
builder.Services.AddScoped<VisualisationsGenerator>();
builder.Services.AddScoped<State>();

var app = builder.Build();

new DbInitializer(app.Configuration).Initialize();


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