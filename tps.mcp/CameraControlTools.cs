using System.ComponentModel;
using System.Text.Json;
using Cysharp.AI;
using ModelContextProtocol.Server;
using tps.client;

namespace tps.mcp;

[McpServerToolType]
public class CameraControlTools(TpsGameApiClient client)
{
    [McpServerTool, Description("Set the player camera pitch (positive=look up, negative=look down). Range: -68.8° to 45.8°.")]
    public async Task<string> SetCameraPitch(
        [Description("Pitch angle in degrees. Positive = look up, negative = look down.")] float pitchDegrees)
    {
        try
        {
            var payload = await client.SetCameraPitchAsync(pitchDegrees);
            var element = JsonSerializer.SerializeToElement(payload);
            return ToonEncoder.Encode(element);
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    [McpServerTool, Description("Make the player camera face a specific world position (sets both yaw and pitch).")]
    public async Task<string> LookAtPosition(
        [Description("World X coordinate of the target")] float x,
        [Description("World Y coordinate of the target")] float y,
        [Description("World Z coordinate of the target")] float z)
    {
        try
        {
            var payload = await client.LookAtPositionAsync(x, y, z);
            var element = JsonSerializer.SerializeToElement(payload);
            return ToonEncoder.Encode(element);
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }
}
