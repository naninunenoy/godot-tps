using System.ComponentModel;
using System.Text.Json;
using gamekit.client;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace gamekit.mcp;

[McpServerToolType]
public class InputSimulationTools(GameApiClient client)
{
    [McpServerTool, Description("Ping the game's input server to check if it is running.")]
    public async Task<string> Ping()
    {
        try
        {
            var payload = await client.PingAsync();
            return payload?.Message ?? "error: empty response";
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [McpServerTool, Description("List all InputMap action names available in the running game.")]
    public async Task<string> GetActions()
    {
        try
        {
            var payload = await client.GetActionsAsync();
            return JsonSerializer.Serialize(payload?.Actions ?? Array.Empty<string>());
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [McpServerTool, Description("Capture a screenshot of the running game and return it as an image.")]
    public async Task<DataContent> TakeScreenshot()
    {
        var bytes = await client.TakeScreenshotAsync();
        return new DataContent(bytes, "image/png");
    }

    [McpServerTool, Description("Simulate pressing a key action defined in the game's InputMap.")]
    public async Task<string> PressAction(
        [Description("The InputMap action name (e.g. 'ui_accept', 'move_forward')")] string action,
        [Description("Duration to hold the action in milliseconds (default 100)")] int durationMs = 100)
    {
        try
        {
            var payload = await client.PressActionAsync(action, durationMs);
            if (payload is null) return "error: empty response";
            return payload.Success ? $"ok: {payload.Message}" : $"error: {payload.Message}";
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }
}
