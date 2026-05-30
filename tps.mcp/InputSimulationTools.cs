using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using tps.contract.Mcp;

namespace tps.mcp;

[McpServerToolType]
public class InputSimulationTools
{
    private static readonly HttpClient _http = new();

    [McpServerTool, Description("Ping the Godot input server to check if it is running.")]
    public static async Task<string> Ping()
    {
        try
        {
            var response = await _http.GetAsync($"{InputEndpoints.BaseUrl}{InputEndpoints.Ping}");
            if (!response.IsSuccessStatusCode)
                return $"error: {response.StatusCode}";

            var payload = await response.Content.ReadFromJsonAsync<PingResponse>();
            return payload?.Message ?? "error: empty response";
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [
        McpServerTool,
        Description("List all InputMap action names available in the running Godot project.")
    ]
    public static async Task<string> GetActions()
    {
        try
        {
            var response = await _http.GetAsync(
                $"{InputEndpoints.BaseUrl}{InputEndpoints.Actions}"
            );
            if (!response.IsSuccessStatusCode)
                return $"error: {response.StatusCode}";

            var payload = await response.Content.ReadFromJsonAsync<GetActionsResponse>();
            return JsonSerializer.Serialize(payload?.Actions ?? Array.Empty<string>());
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [
        McpServerTool,
        Description("Capture a screenshot of the running Godot game and return it as an image.")
    ]
    public static async Task<DataContent> TakeScreenshot()
    {
        var response = await _http.GetAsync($"{InputEndpoints.BaseUrl}{InputEndpoints.Screenshot}");
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return new DataContent(bytes, "image/png");
    }

    [McpServerTool, Description("Enable or disable ADS (Aim Down Sights) state. Persists until explicitly changed.")]
    public static async Task<string> SetAiming(
        [Description("true to enter ADS, false to exit ADS")] bool isAiming)
    {
        var request = new SetAimingRequest(isAiming);
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        try
        {
            var response = await _http.PostAsync(
                $"{InputEndpoints.BaseUrl}{InputEndpoints.SetAiming}",
                content
            );
            if (!response.IsSuccessStatusCode)
                return $"error: {response.StatusCode}";

            var payload = await response.Content.ReadFromJsonAsync<PressActionResponse>();
            if (payload is null)
                return "error: empty response";

            return payload.Success ? $"ok: {payload.Message}" : $"error: {payload.Message}";
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [McpServerTool, Description("Simulate pressing a key action defined in Godot's InputMap.")]
    public static async Task<string> PressAction(
        [Description("The InputMap action name (e.g. 'ui_accept', 'move_forward')")] string action,
        [Description("Duration to hold the action in milliseconds (default 100)")]
            int durationMs = 100
    )
    {
        var request = new PressActionRequest(action, durationMs);
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        try
        {
            var response = await _http.PostAsync(
                $"{InputEndpoints.BaseUrl}{InputEndpoints.PressAction}",
                content
            );
            if (!response.IsSuccessStatusCode)
                return $"error: {response.StatusCode}";

            var payload = await response.Content.ReadFromJsonAsync<PressActionResponse>();
            if (payload is null)
                return "error: empty response";

            return payload.Success ? $"ok: {payload.Message}" : $"error: {payload.Message}";
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }
}
