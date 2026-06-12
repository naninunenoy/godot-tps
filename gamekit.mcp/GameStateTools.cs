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
    public Task<string> GetGameState() =>
        McpToolRunner.RunAsync(async () =>
        {
            // ペイロード型はゲーム定義のため、素の JSON を中継して ToonEncoder へ渡す
            var json = await client.GetStateRawAsync();
            using var doc = JsonDocument.Parse(json);
            return ToonEncoder.Encode(doc.RootElement);
        });

    [McpServerTool, Description("Get available commands in the current game scene.")]
    public Task<string> GetAvailableCommands() =>
        McpToolRunner.EncodeAsync(() => client.GetAvailableCommandsAsync());
}
