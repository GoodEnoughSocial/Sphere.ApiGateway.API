using Consul;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

using var client = new ConsulClient();

var registration = new AgentServiceRegistration
{
    ID = Guid.NewGuid().ToString(),
    Name = "apigateway",
    Port = 5003,
    Address = "localhost",
};

var result = await client.Agent.ServiceRegister(registration);

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
        s.AddOcelot()
        .AddConsul()
        .AddConfigStoredInConsul();
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.AddConsole();
    })
    .Configure(app =>
    {
        app.UseOcelot().Wait();
    })
    .Build()
    .Run();

await client.Agent.ServiceDeregister(registration.ID);
