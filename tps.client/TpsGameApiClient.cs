using System.Net.Http.Json;
using gamekit.client;
using tps.contract.Mcp;

namespace tps.client;

public class TpsGameApiClient(HttpClient http) : GameApiClient(http)
{
    public Task<GameStateResponse?> GetStateAsync() => GetStateAsync<GameStateResponse>();

    public async Task<CameraControlResponse?> SetAimingAsync(bool isAiming)
    {
        var content = Serialize(new SetAimingRequest(isAiming));
        var response = await Http.PostAsync(TpsEndpoints.SetAiming, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CameraControlResponse>();
    }

    public async Task<CameraControlResponse?> SetCameraPitchAsync(float pitchDegrees)
    {
        var content = Serialize(new SetCameraPitchRequest(pitchDegrees));
        var response = await Http.PostAsync(TpsEndpoints.CameraPitch, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CameraControlResponse>();
    }

    public async Task<CameraControlResponse?> LookAtPositionAsync(float x, float y, float z)
    {
        var content = Serialize(new LookAtPositionRequest(x, y, z));
        var response = await Http.PostAsync(TpsEndpoints.LookAt, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CameraControlResponse>();
    }
}
