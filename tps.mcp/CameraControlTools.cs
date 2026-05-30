using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Cysharp.AI;
using ModelContextProtocol.Server;
using tps.contract.Mcp;

namespace tps.mcp;

[McpServerToolType]
public class CameraControlTools
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    [McpServerTool, Description("Set the player camera pitch (positive=look down, negative=look up). Range: -68.8° to 45.8°.")]
    public static async Task<string> SetCameraPitch(
        [Description("Pitch angle in degrees. Positive = look down, negative = look up.")] float pitchDegrees)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new SetCameraPitchRequest(pitchDegrees)),
            Encoding.UTF8,
            "application/json"
        );
        try
        {
            var response = await _http.PostAsync(
                $"{InputEndpoints.BaseUrl}{InputEndpoints.CameraPitch}",
                content
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

    [McpServerTool, Description("Make the player camera face a specific world position (sets both yaw and pitch).")]
    public static async Task<string> LookAtPosition(
        [Description("World X coordinate of the target")] float x,
        [Description("World Y coordinate of the target")] float y,
        [Description("World Z coordinate of the target")] float z)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new LookAtPositionRequest(x, y, z)),
            Encoding.UTF8,
            "application/json"
        );
        try
        {
            var response = await _http.PostAsync(
                $"{InputEndpoints.BaseUrl}{InputEndpoints.LookAt}",
                content
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
