using System.ComponentModel;
using System.Text.Json;
using Cysharp.AI;
using gamekit.client;
using ModelContextProtocol.Server;

namespace gamekit.mcp;

[McpServerToolType]
public class GameStateTools(GameApiClient client)
{
    [McpServerTool, Description("Get current game state (entities and their components, frame count).")]
    public async Task<string> GetGameState()
    {
        try
        {
            // ペイロード型はゲーム定義のため、素の JSON を中継して ToonEncoder へ渡す
            var json = await client.GetStateRawAsync();
            using var doc = JsonDocument.Parse(json);
            return ToonEncoder.Encode(doc.RootElement);
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
