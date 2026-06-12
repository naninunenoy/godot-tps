using System;
using System.Linq;
using gamekit.contract.Mcp;
using gamekit.godot.Logging;
using Godot;
using Microsoft.Extensions.Logging;

namespace gamekit.godot;

/// <summary>
/// どのゲームでも共通の組み込みルートを GameHttpServer に登録する。
/// /state のペイロード型はゲーム定義のため、stateBuilder（ISceneQuery → DTO）を注入する。
/// sceneProvider / sceneQueryProvider が null を返す間（未初期化）は 503 を返す。
/// </summary>
public static class GameApiRoutes
{
    private static readonly ILogger Logger = AppLogger.For("gamekit.godot.GameApiRoutes");

    public static void Register(
        GameHttpServer server,
        Func<IScene?> sceneProvider,
        Func<ISceneQuery?> sceneQueryProvider,
        Func<ISceneQuery, object> stateBuilder
    )
    {
        var tree = server.Tree;

        server.MapGet(InputEndpoints.Ping, () => HttpResult.Json(new PingResponse("pong")));

        server.MapGet(
            InputEndpoints.Actions,
            () =>
            {
                var actions = InputMap.GetActions().Select(a => a.ToString()).ToArray();
                return HttpResult.Json(new GetActionsResponse(actions));
            }
        );

        server.MapPostJson<PressActionRequest>(
            InputEndpoints.PressAction,
            request => HandlePressAction(tree, request)
        );

        server.MapGet(
            InputEndpoints.Screenshot,
            async () =>
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                var image = tree.Root.GetTexture().GetImage();
                var pngBytes = image.SavePngToBuffer();
                Logger.LogDebug("Screenshot captured ({Bytes} bytes)", pngBytes.Length);
                return HttpResult.Binary(pngBytes, "image/png");
            }
        );

        server.MapGet(
            InputEndpoints.Commands,
            () =>
            {
                var scene = sceneProvider();
                if (scene is null)
                    return HttpResult.Text("not initialized", 503);
                var commands = scene.AvailableCommands.Select(c => c.Name).ToArray();
                return HttpResult.Json(new CommandListResponse(commands));
            }
        );

        server.MapGet(
            InputEndpoints.State,
            () =>
            {
                var sceneQuery = sceneQueryProvider();
                return sceneQuery is null
                    ? HttpResult.Text("not initialized", 503)
                    : HttpResult.Json(stateBuilder(sceneQuery));
            }
        );
    }

    private static HttpResult HandlePressAction(SceneTree tree, PressActionRequest request)
    {
        if (string.IsNullOrEmpty(request.Action))
            return HttpResult.Json(new PressActionResponse(false, "invalid request"));

        if (!InputMap.HasAction(request.Action))
            return HttpResult.Json(new PressActionResponse(false, $"unknown action: {request.Action}"));

        Logger.LogDebug("ActionPress: {Action} ({DurationMs}ms)", request.Action, request.DurationMs);
        Input.ActionPress(request.Action);
        ReleaseActionAfterDelay(tree, request.Action, request.DurationMs);

        return HttpResult.Json(
            new PressActionResponse(true, $"pressed {request.Action} for {request.DurationMs}ms")
        );
    }

    // async void のため、ここから例外を漏らすとプロセスごと落ちる（終了中のツリー破棄等）
    private static async void ReleaseActionAfterDelay(SceneTree tree, string action, int durationMs)
    {
        try
        {
            await tree.ToSignal(
                tree.CreateTimer(durationMs / 1000.0),
                SceneTreeTimer.SignalName.Timeout
            );
            Input.ActionRelease(action);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ActionRelease failed: {Action}", action);
        }
    }
}
