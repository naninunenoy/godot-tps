using ConsoleAppFramework;
using System.Text.Json;
using Cysharp.AI;
using gamekit.client;

/// <summary>どのゲームでも使える汎用コマンド。ゲーム固有コマンドは TpsCommands を参照。</summary>
public class GameCommands
{
    private readonly GameApiClient _client = new(new HttpClient());

    /// <summary>ゲームサーバーの疎通確認</summary>
    public async Task Ping()
    {
        var r = await _client.PingAsync();
        Console.WriteLine(r?.Message ?? "no response");
    }

    /// <summary>ゲームの現在状態を取得</summary>
    public async Task State()
    {
        var json = await _client.GetStateRawAsync();
        using var doc = JsonDocument.Parse(json);
        Console.WriteLine(ToonEncoder.Encode(doc.RootElement));
    }

    /// <summary>現在シーンで使えるコマンド一覧を取得</summary>
    public async Task Commands()
    {
        var r = await _client.GetAvailableCommandsAsync();
        Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
    }

    /// <summary>InputMap のアクション名一覧を取得</summary>
    public async Task Actions()
    {
        var r = await _client.GetActionsAsync();
        foreach (var a in r?.Actions ?? [])
            Console.WriteLine(a);
    }

    /// <summary>InputMap アクションを押す</summary>
    /// <param name="action">InputMap アクション名 (例: move_forward)</param>
    /// <param name="ms">ボタンを押す時間 (ms)</param>
    public async Task<int> Press([Argument] string action, int ms = 100)
    {
        var r = await _client.PressActionAsync(action, ms);
        if (r is null) { Console.Error.WriteLine("error: no response"); return 1; }
        Console.WriteLine(r.Success ? $"ok: {r.Message}" : $"error: {r.Message}");
        return r.Success ? 0 : 1;
    }

    /// <summary>ゲームのスクリーンショットを撮る</summary>
    /// <param name="output">保存先ファイルパス</param>
    public async Task Screenshot(string output = "screenshot.png")
    {
        var bytes = await _client.TakeScreenshotAsync();
        await File.WriteAllBytesAsync(output, bytes);
        Console.WriteLine($"saved: {output} ({bytes.Length} bytes)");
    }
}
