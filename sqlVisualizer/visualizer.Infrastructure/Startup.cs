using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using sql.executor;

namespace visualizer.Infrastructure;

public static class Startup
{
    public static void ConfigureExternalServices(IServiceCollection services, IConfiguration config)
    {
        commonDataModels.ConfigureService.Configure(services);
        sql.executor.ConfigureService.Configure(services);
        inputParsing.ConfigureService.Configure(services);
        tableGeneration.ConfigureService.Configure(services);
        animationGeneration.ConfigureService.Configure(services);
        visualizer.service.ConfigureService.Configure(services);
    }

    public static void ConfigurePipeline(WebApplication app)
    {
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
        new DbInitializer(app.Configuration).InitializePreTestDB();
        new DbInitializer(app.Configuration).InitializePostTestDB();

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
    }
}
