using System.ComponentModel;
using System.Text.Json;
using Cysharp.AI;
using ModelContextProtocol.Server;
using tps.contract.Mcp;

namespace tps.mcp;

[McpServerToolType]
public class GameStateTools
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    [McpServerTool, Description("Get current game state (entities, health, weapons, frame count).")]
    public static async Task<string> GetGameState()
    {
        try
        {
            var response = await _http.GetAsync($"{InputEndpoints.BaseUrl}{InputEndpoints.State}");
            if (!response.IsSuccessStatusCode)
                return $"error: {response.StatusCode}";

            var json = await response.Content.ReadAsStringAsync();
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return ToonEncoder.Encode(element);
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get available commands in the current game scene.")]
    public static async Task<string> GetAvailableCommands()
    {
        try
        {
            var response = await _http.GetAsync(
                $"{InputEndpoints.BaseUrl}{InputEndpoints.Commands}"
            );
            if (!response.IsSuccessStatusCode)
                return $"error: {response.StatusCode}";

            var json = await response.Content.ReadAsStringAsync();
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return ToonEncoder.Encode(element);
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }
}
