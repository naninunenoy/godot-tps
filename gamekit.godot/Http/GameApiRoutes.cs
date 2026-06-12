using System;
using System.Linq;
using gamekit.contract.Mcp;
using Godot;

namespace gamekit.godot;

/// <summary>
/// どのゲームでも共通の組み込みルートを GameHttpServer に登録する。
/// /state のペイロードはゲーム定義のため、stateProvider 経由で注入する
/// （未初期化なら null を返すこと。503 を返す）。
/// </summary>
public static class GameApiRoutes
{
    public static void Register(
        GameHttpServer server,
        SceneTree tree,
        Func<IScene?> sceneProvider,
        Func<object?> stateProvider
    )
    {
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
                GD.Print($"[GameApiRoutes] Screenshot captured ({pngBytes.Length} bytes)");
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
                var state = stateProvider();
                return state is null
                    ? HttpResult.Text("not initialized", 503)
                    : HttpResult.Json(state);
            }
        );
    }

    private static HttpResult HandlePressAction(SceneTree tree, PressActionRequest request)
    {
        if (string.IsNullOrEmpty(request.Action))
            return HttpResult.Json(new PressActionResponse(false, "invalid request"));

        if (!InputMap.HasAction(request.Action))
            return HttpResult.Json(new PressActionResponse(false, $"unknown action: {request.Action}"));

        GD.Print($"[GameApiRoutes] ActionPress: {request.Action} ({request.DurationMs}ms)");
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
            GD.PrintErr($"[GameApiRoutes] ActionRelease failed: {ex.Message}");
        }
    }
}
