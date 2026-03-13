#if DEBUG
#define TRACE_LOGGING
#endif

using Microsoft.AspNetCore.DataProtection;
using SimpleFluidd.Components;
using SimpleFluidd.Configuration;
using SimpleFluidd.Services;

namespace SimpleFluidd;

public static class Program
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "ASP0011:Suggest using builder.Logging over Host.ConfigureLogging or WebHost.ConfigureLogging", Justification = "It doesn't work otherwise.")]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables();

        var containerConfig = EnvironmentVariableMapper.MapTo<ContainerConfig>();
        builder.Configuration.Bind(containerConfig);
        builder.Services.AddSingleton(containerConfig);

        builder.Host.ConfigureLogging(logOptions =>
        {
            logOptions.ClearProviders();
            logOptions.AddConsole();
            logOptions.AddDebug();

            LogLevel level =
#if TRACE_LOGGING
            LogLevel.Trace;
#else
            containerConfig.LogLevel;
#endif
            logOptions.SetMinimumLevel(level);

            logOptions.AddFilter("Microsoft", LogLevel.Warning);
            logOptions.AddFilter("SimpleFluidd", level);
        });

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddRazorPages();

        builder.Services.AddBlazorBootstrap();

        builder.Services.AddSingleton(sp =>
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Add("UserAgent", "SimpleFluidd, Minimal print scrape");
            return client;
        });

        builder.Services.AddSingleton<PrinterCoordinator>()
            .AddHostedService(sp => sp.GetRequiredService<PrinterCoordinator>())
            .AddSingleton<IPrinterService, PrinterService>();

        builder.Services.AddDataProtection()
            .UseEphemeralDataProtectionProvider();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
