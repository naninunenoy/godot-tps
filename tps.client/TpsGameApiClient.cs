using gamekit.client;
using tps.contract.Mcp;

namespace tps.client;

public class TpsGameApiClient(HttpClient http) : GameApiClient(http)
{
    public Task<GameStateResponse?> GetStateAsync() => GetStateAsync<GameStateResponse>();

    public Task<CameraControlResponse?> SetAimingAsync(bool isAiming) =>
        PostJsonAsync<SetAimingRequest, CameraControlResponse>(
            TpsEndpoints.SetAiming,
            new SetAimingRequest(isAiming)
        );

    public Task<CameraControlResponse?> SetCameraPitchAsync(float pitchDegrees) =>
        PostJsonAsync<SetCameraPitchRequest, CameraControlResponse>(
            TpsEndpoints.CameraPitch,
            new SetCameraPitchRequest(pitchDegrees)
        );

    public Task<CameraControlResponse?> LookAtPositionAsync(float x, float y, float z) =>
        PostJsonAsync<LookAtPositionRequest, CameraControlResponse>(
            TpsEndpoints.LookAt,
            new LookAtPositionRequest(x, y, z)
        );
}
