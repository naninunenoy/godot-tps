using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using tps.client;
using tps.mcp;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient<GameApiClient>();
builder
    .Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<InputSimulationTools>()
    .WithTools<GameStateTools>()
    .WithTools<CameraControlTools>();

await builder.Build().RunAsync();
