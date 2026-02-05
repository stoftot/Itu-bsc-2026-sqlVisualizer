using Microsoft.Data.Sqlite;
using visualizer;
using visualizer.Components;
using visualizer.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<SqliteConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connString = config.GetConnectionString("Default");
    return new SqliteConnection(connString);
});

var app = builder.Build();

new DbInitializer(app.Configuration).Initialize();

using var scope = app.Services.CreateScope();
var conn = scope.ServiceProvider.GetRequiredService<SqliteConnection>();

var sqlExecutor = new SQLExecutor(conn);
var query = "SELECT * FROM shift";
var table = await sqlExecutor.Execute(query);
Console.WriteLine($"Query: {query}");
foreach (var row in table.Entries){
    Console.WriteLine(string.Join(", ", row));
}

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