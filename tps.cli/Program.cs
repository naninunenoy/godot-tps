using ConsoleAppFramework;
using System.Text.Json;
using Cysharp.AI;
using tps.client;

var http = new HttpClient();
var client = new GameApiClient(http);

var app = ConsoleApp.Create();
app.UseFilter<ErrorFilter>();

app.Add("ping", async () =>
{
    var r = await client.PingAsync();
    Console.WriteLine(r?.Message ?? "no response");
});

app.Add("state", async () =>
{
    var r = await client.GetStateAsync();
    Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
});

app.Add("commands", async () =>
{
    var r = await client.GetAvailableCommandsAsync();
    Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
});

app.Add("actions", async () =>
{
    var r = await client.GetActionsAsync();
    foreach (var a in r?.Actions ?? [])
        Console.WriteLine(a);
});

app.Add("press", async Task<int> ([Argument] string action, int ms = 100) =>
{
    var r = await client.PressActionAsync(action, ms);
    if (r is null) { Console.Error.WriteLine("error: no response"); return 1; }
    Console.WriteLine(r.Success ? $"ok: {r.Message}" : $"error: {r.Message}");
    return r.Success ? 0 : 1;
});

app.Add("aim", async Task<int> ([Argument] bool value) =>
{
    var r = await client.SetAimingAsync(value);
    if (r is null) { Console.Error.WriteLine("error: no response"); return 1; }
    Console.WriteLine(r.Success ? $"ok: {r.Message}" : $"error: {r.Message}");
    return r.Success ? 0 : 1;
});

app.Add("pitch", async ([Argument] float degrees) =>
{
    var r = await client.SetCameraPitchAsync(degrees);
    Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
});

app.Add("look-at", async ([Argument] float x, [Argument] float y, [Argument] float z) =>
{
    var r = await client.LookAtPositionAsync(x, y, z);
    Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
});

app.Add("screenshot", async (string output = "screenshot.png") =>
{
    var bytes = await client.TakeScreenshotAsync();
    await File.WriteAllBytesAsync(output, bytes);
    Console.WriteLine($"saved: {output} ({bytes.Length} bytes)");
});

app.Run(args);

class ErrorFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        try
        {
            await next.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
