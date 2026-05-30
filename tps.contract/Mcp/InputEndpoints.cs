namespace tps.contract.Mcp;

public static class InputEndpoints
{
    public const int Port = 9876;
    public const string BaseUrl = "http://localhost:9876";

    public const string Ping = "/ping";
    public const string Actions = "/actions";
    public const string PressAction = "/press_action";
    public const string Screenshot = "/screenshot";
    public const string State = "/state";
    public const string Commands = "/commands";
    public const string CameraPitch = "/camera_pitch";
    public const string LookAt = "/look_at";
}
