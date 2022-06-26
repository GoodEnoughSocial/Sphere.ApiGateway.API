using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Serilog;
using Sphere.Shared;

// Setting this allows us to get some benefits all over the place.
Services.Current = Services.ApiGateway;

Log.Logger = SphericalLogger.StartupLogger(Services.Current);

try
{
    new WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config
             .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
             .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
             .AddCommandLine(args)
             .AddEnvironmentVariables();
        })
        .ConfigureServices(s =>
        {
            s.AddHealthChecks();

            s.AddOcelot()
            .AddConsul()
            .AddConfigStoredInConsul();
        })
        .ConfigureLogging((hostingContext, logging) =>
        {
            logging.AddSerilog(Log.Logger);
        })
        .Configure(app =>
        {
            //app.MapHealthChecks(Constants.HealthCheckEndpoint);
            app.UseOcelot().Wait();
        })
        .Build()
        .Run();
}
catch (Exception ex)
{
    if (ex.GetType().Name != "StopTheHostException")
    {
        Log.Fatal(ex, "Unhandled exception");
    }
}
finally
{
    Log.Information("Shutting down");
    Log.CloseAndFlush();
}
