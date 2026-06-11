using gamekit.client;
using gamekit.mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using tps.client;
using tps.mcp;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient<TpsGameApiClient>();
// 汎用ツールは基底 GameApiClient を要求するため、同じ TpsGameApiClient へ forward する
// （別々に typed client 登録すると HttpClient が二重になる）
builder.Services.AddTransient<GameApiClient>(sp => sp.GetRequiredService<TpsGameApiClient>());
builder
    .Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<InputSimulationTools>()
    .WithTools<GameStateTools>()
    .WithTools<CameraControlTools>();

await builder.Build().RunAsync();
