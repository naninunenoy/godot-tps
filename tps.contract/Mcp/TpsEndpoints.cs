namespace tps.contract.Mcp;

/// <summary>TPS 固有のエンドポイント。汎用エンドポイントは gamekit.contract の InputEndpoints を参照。</summary>
public static class TpsEndpoints
{
    public const string CameraPitch = "/camera_pitch";
    public const string LookAt = "/look_at";
    public const string SetAiming = "/set_aiming";
}
