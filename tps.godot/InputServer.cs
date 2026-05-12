using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using tps.contract;
using tps.csharp;

public partial class InputServer : Node
{
    private readonly TcpServer _tcpServer = new();
    private ISceneQuery? _sceneQuery;
    private IScene? _scene;

    public void Initialize(ISceneQuery sceneQuery, IScene scene)
    {
        _sceneQuery = sceneQuery;
        _scene = scene;
    }

    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
            return;

        var err = _tcpServer.Listen(InputEndpoints.Port);
        if (err != Error.Ok)
            GD.PrintErr($"[InputServer] Failed to listen on port {InputEndpoints.Port}: {err}");
        else
            GD.Print($"[InputServer] Listening on port {InputEndpoints.Port}");
    }

    public override void _ExitTree()
    {
        _tcpServer.Stop();
    }

    public override void _Process(double delta)
    {
        if (_tcpServer.IsConnectionAvailable())
            HandleConnectionAsync(_tcpServer.TakeConnection());
    }

    private async void HandleConnectionAsync(StreamPeerTcp peer)
    {
        try
        {
            var (method, path, body) = await ReadRequestAsync(peer);
            GD.PrintRich($"[InputServer] {method} {path}");

            if (method == "GET" && path == InputEndpoints.Ping)
            {
                SendJsonResponse(peer, 200, new PingResponse("pong"));
            }
            else if (method == "GET" && path == InputEndpoints.Actions)
            {
                var actions = InputMap.GetActions().Select(a => a.ToString()).ToArray();
                SendJsonResponse(peer, 200, new GetActionsResponse(actions));
            }
            else if (method == "POST" && path == InputEndpoints.PressAction)
            {
                var response = await HandlePressActionAsync(body);
                SendJsonResponse(peer, 200, response);
            }
            else if (method == "GET" && path == InputEndpoints.Screenshot)
            {
                var imageBytes = await HandleScreenshotAsync();
                SendBinaryResponse(peer, 200, imageBytes, "image/png");
            }
            else if (method == "GET" && path == InputEndpoints.State)
            {
                if (_sceneQuery is null)
                    SendTextResponse(peer, 503, "not initialized");
                else
                    SendJsonResponse(peer, 200, BuildStateResponse());
            }
            else if (method == "GET" && path == InputEndpoints.Commands)
            {
                if (_scene is null)
                    SendTextResponse(peer, 503, "not initialized");
                else
                    SendJsonResponse(peer, 200, new CommandListResponse(
                        _scene.AvailableCommands.Select(c => c.Name).ToArray()));
            }
            else
            {
                SendTextResponse(peer, 404, $"not found: {path}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[InputServer] {ex.Message}");
        }
        finally
        {
            peer.DisconnectFromHost();
        }
    }

    private async Task<(string method, string path, string body)> ReadRequestAsync(StreamPeerTcp peer)
    {
        var rawBytes = new List<byte>();
        var headerEnd = -1;

        while (headerEnd < 0)
        {
            peer.Poll();
            var available = peer.GetAvailableBytes();
            if (available > 0)
            {
                var result = peer.GetData(available);
                if (result[0].As<Error>() == Error.Ok)
                    rawBytes.AddRange(result[1].AsByteArray());

                for (var i = 0; i <= rawBytes.Count - 4; i++)
                {
                    if (rawBytes[i] == '\r' && rawBytes[i + 1] == '\n' &&
                        rawBytes[i + 2] == '\r' && rawBytes[i + 3] == '\n')
                    {
                        headerEnd = i;
                        break;
                    }
                }
            }

            if (headerEnd < 0)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        var headerStr = Encoding.UTF8.GetString(rawBytes.Take(headerEnd).ToArray());
        var lines = headerStr.Split("\r\n");
        var requestParts = lines[0].Split(' ');
        var method = requestParts[0];
        var path = requestParts[1];

        var contentLength = 0;
        foreach (var line in lines.Skip(1))
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                contentLength = int.Parse(line.Split(':')[1].Trim());
        }

        var bodyBytes = rawBytes.Skip(headerEnd + 4).ToList();
        while (bodyBytes.Count < contentLength)
        {
            peer.Poll();
            var available = peer.GetAvailableBytes();
            if (available > 0)
            {
                var result = peer.GetData(available);
                if (result[0].As<Error>() == Error.Ok)
                    bodyBytes.AddRange(result[1].AsByteArray());
            }
            else
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        return (method, path, Encoding.UTF8.GetString(bodyBytes.Take(contentLength).ToArray()));
    }

    private async Task<PressActionResponse> HandlePressActionAsync(string body)
    {
        var request = JsonSerializer.Deserialize<PressActionRequest>(body);
        if (request is null || string.IsNullOrEmpty(request.Action))
            return new PressActionResponse(false, "invalid request");

        if (!InputMap.HasAction(request.Action))
            return new PressActionResponse(false, $"unknown action: {request.Action}");

        GD.Print($"[InputServer] ActionPress: {request.Action} ({request.DurationMs}ms)");
        Input.ActionPress(request.Action);
        await ToSignal(GetTree().CreateTimer(request.DurationMs / 1000.0), SceneTreeTimer.SignalName.Timeout);
        Input.ActionRelease(request.Action);

        return new PressActionResponse(true, $"pressed {request.Action} for {request.DurationMs}ms");
    }

    private async Task<byte[]> HandleScreenshotAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var image = GetViewport().GetTexture().GetImage();
        var pngBytes = image.SavePngToBuffer();
        GD.Print($"[InputServer] Screenshot captured ({pngBytes.Length} bytes)");
        return pngBytes;
    }

    private static void SendJsonResponse<T>(StreamPeerTcp peer, int status, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var bodyBytes = Encoding.UTF8.GetBytes(json);
        WriteResponse(peer, status, bodyBytes, "application/json; charset=utf-8");
    }

    private static void SendTextResponse(StreamPeerTcp peer, int status, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        WriteResponse(peer, status, bodyBytes, "text/plain; charset=utf-8");
    }

    private static void SendBinaryResponse(StreamPeerTcp peer, int status, byte[] bodyBytes, string contentType)
    {
        WriteResponse(peer, status, bodyBytes, contentType);
    }

    private static void WriteResponse(StreamPeerTcp peer, int status, byte[] bodyBytes, string contentType)
    {
        var statusText = status switch { 200 => "OK", 404 => "Not Found", _ => "Error" };
        var header = $"HTTP/1.1 {status} {statusText}\r\nContent-Type: {contentType}\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n";
        peer.PutData(Encoding.UTF8.GetBytes(header).Concat(bodyBytes).ToArray());
    }

    private GameStateResponse BuildStateResponse()
    {
        var objects = _sceneQuery!.Snapshot.Select(obj => new ObjectSnapshotDto(
            obj.Id.AsPrimitive(),
            obj.Name,
            obj.GetComponent<HealthComponent>() is { } h ? new HealthDto(h.Hp, h.MaxHp) : null,
            obj.GetComponent<WeaponComponent>() is { } w ? new WeaponDto(w.Ammo, w.MagazineSize, w.IsReloading) : null
        )).ToArray();
        return new GameStateResponse(_sceneQuery.FrameCount, _sceneQuery.ObjectCount, objects);
    }
}
