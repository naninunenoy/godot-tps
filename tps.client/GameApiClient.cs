using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using gamekit.contract.Mcp;
using tps.contract.Mcp;

namespace tps.client;

public class GameApiClient
{
    private readonly HttpClient _http;

    public GameApiClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri(InputEndpoints.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<PingResponse?> PingAsync()
    {
        var response = await _http.GetAsync(InputEndpoints.Ping);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PingResponse>();
    }

    public async Task<GetActionsResponse?> GetActionsAsync()
    {
        var response = await _http.GetAsync(InputEndpoints.Actions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetActionsResponse>();
    }

    public async Task<byte[]> TakeScreenshotAsync()
    {
        var response = await _http.GetAsync(InputEndpoints.Screenshot);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<PressActionResponse?> PressActionAsync(string action, int durationMs)
    {
        var content = Serialize(new PressActionRequest(action, durationMs));
        var response = await _http.PostAsync(InputEndpoints.PressAction, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PressActionResponse>();
    }

    public async Task<CameraControlResponse?> SetAimingAsync(bool isAiming)
    {
        var content = Serialize(new SetAimingRequest(isAiming));
        var response = await _http.PostAsync(TpsEndpoints.SetAiming, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CameraControlResponse>();
    }

    public async Task<GameStateResponse?> GetStateAsync()
    {
        var response = await _http.GetAsync(InputEndpoints.State);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GameStateResponse>();
    }

    public async Task<CommandListResponse?> GetAvailableCommandsAsync()
    {
        var response = await _http.GetAsync(InputEndpoints.Commands);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandListResponse>();
    }

    public async Task<CameraControlResponse?> SetCameraPitchAsync(float pitchDegrees)
    {
        var content = Serialize(new SetCameraPitchRequest(pitchDegrees));
        var response = await _http.PostAsync(TpsEndpoints.CameraPitch, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CameraControlResponse>();
    }

    public async Task<CameraControlResponse?> LookAtPositionAsync(float x, float y, float z)
    {
        var content = Serialize(new LookAtPositionRequest(x, y, z));
        var response = await _http.PostAsync(TpsEndpoints.LookAt, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CameraControlResponse>();
    }

    private static StringContent Serialize<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
