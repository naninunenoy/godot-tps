using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace tps.mcp;

[McpServerToolType]
public class InputSimulationTools
{
    private static readonly HttpClient _http = new();
    private const string GodotBaseUrl = "http://localhost:9876";

    [McpServerTool, Description("Ping the Godot input server to check if it is running.")]
    public static async Task<string> Ping()
    {
        try
        {
            var response = await _http.GetAsync($"{GodotBaseUrl}/ping");
            return response.IsSuccessStatusCode ? "pong" : $"error: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [McpServerTool, Description("List all InputMap action names available in the running Godot project.")]
    public static async Task<string> GetActions()
    {
        try
        {
            var response = await _http.GetAsync($"{GodotBaseUrl}/actions");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [McpServerTool, Description("Capture a screenshot of the running Godot game and return it as an image.")]
    public static async Task<DataContent> TakeScreenshot()
    {
        var response = await _http.GetAsync($"{GodotBaseUrl}/screenshot");
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return new DataContent(bytes, "image/png");
    }

    [McpServerTool, Description("Simulate pressing a key action defined in Godot's InputMap.")]
    public static async Task<string> PressAction(
        [Description("The InputMap action name (e.g. 'ui_accept', 'move_forward')")] string action,
        [Description("Duration to hold the action in milliseconds (default 100)")] int durationMs = 100)
    {
        var payload = JsonSerializer.Serialize(new { action, durationMs });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        try
        {
            var response = await _http.PostAsync($"{GodotBaseUrl}/press_action", content);
            var body = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode ? $"ok: {body}" : $"error: {body}";
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }
}
