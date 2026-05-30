using System.ComponentModel;
using System.Text.Json;
using Cysharp.AI;
using ModelContextProtocol.Server;
using tps.client;

namespace tps.mcp;

[McpServerToolType]
public class GameStateTools(GameApiClient client)
{
    [McpServerTool, Description("Get current game state (entities, health, weapons, frame count).")]
    public async Task<string> GetGameState()
    {
        try
        {
            var payload = await client.GetStateAsync();
            var element = JsonSerializer.SerializeToElement(payload);
            return ToonEncoder.Encode(element);
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get available commands in the current game scene.")]
    public async Task<string> GetAvailableCommands()
    {
        try
        {
            var payload = await client.GetAvailableCommandsAsync();
            var element = JsonSerializer.SerializeToElement(payload);
            return ToonEncoder.Encode(element);
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }
}
