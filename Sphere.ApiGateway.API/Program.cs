using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Serilog;
using Sphere.Shared;

Log.Logger = SphericalLogger.SetupLogger();

Log.Information("Starting up");

var registration = Services.ApiGateway.GetServiceRegistration();

try
{
    var result = await Services.RegisterService(registration);

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
            //app.MapHealthChecks("/health");
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
    await Services.UnregisterService(registration);

    Log.Information("Shutting down");
    Log.CloseAndFlush();
}
