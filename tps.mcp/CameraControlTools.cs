using System.ComponentModel;
using gamekit.mcp;
using ModelContextProtocol.Server;
using tps.client;

namespace tps.mcp;

[McpServerToolType]
public class CameraControlTools(TpsGameApiClient client)
{
    [McpServerTool, Description("Set the player camera pitch (positive=look up, negative=look down). Range: -68.8° to 45.8°.")]
    public Task<string> SetCameraPitch(
        [Description("Pitch angle in degrees. Positive = look up, negative = look down.")] float pitchDegrees) =>
        McpToolRunner.EncodeAsync(() => client.SetCameraPitchAsync(pitchDegrees));

    [McpServerTool, Description("Make the player camera face a specific world position (sets both yaw and pitch).")]
    public Task<string> LookAtPosition(
        [Description("World X coordinate of the target")] float x,
        [Description("World Y coordinate of the target")] float y,
        [Description("World Z coordinate of the target")] float z) =>
        McpToolRunner.EncodeAsync(() => client.LookAtPositionAsync(x, y, z));

    [McpServerTool, Description("Enable or disable ADS (Aim Down Sights) state. Persists until explicitly changed.")]
    public Task<string> SetAiming(
        [Description("true to enter ADS, false to exit ADS")] bool isAiming) =>
        McpToolRunner.RunAsync(async () =>
        {
            var payload = await client.SetAimingAsync(isAiming);
            if (payload is null) return "error: empty response";
            return payload.Success ? $"ok: {payload.Message}" : $"error: {payload.Message}";
        });
}
