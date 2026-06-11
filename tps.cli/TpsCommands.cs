using ConsoleAppFramework;
using System.Text.Json;
using Cysharp.AI;
using tps.client;

public class TpsCommands
{
    private readonly TpsGameApiClient _client = new(new HttpClient());

    /// <summary>ゲームサーバーの疎通確認</summary>
    public async Task Ping()
    {
        var r = await _client.PingAsync();
        Console.WriteLine(r?.Message ?? "no response");
    }

    /// <summary>ゲームの現在状態を取得</summary>
    public async Task State()
    {
        var r = await _client.GetStateAsync();
        Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
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

    /// <summary>ADS (照準) 状態を設定</summary>
    /// <param name="value">true=ADS ON / false=ADS OFF</param>
    public async Task<int> Aim([Argument] bool value)
    {
        var r = await _client.SetAimingAsync(value);
        if (r is null) { Console.Error.WriteLine("error: no response"); return 1; }
        Console.WriteLine(r.Success ? $"ok: {r.Message}" : $"error: {r.Message}");
        return r.Success ? 0 : 1;
    }

    /// <summary>カメラのピッチ角を設定</summary>
    /// <param name="degrees">ピッチ角度 (上=正 / 下=負, 範囲: -68.8 〜 45.8)</param>
    public async Task Pitch([Argument] float degrees)
    {
        var r = await _client.SetCameraPitchAsync(degrees);
        Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
    }

    /// <summary>指定ワールド座標の方向へカメラを向ける</summary>
    /// <param name="x">ワールド座標 X</param>
    /// <param name="y">ワールド座標 Y</param>
    /// <param name="z">ワールド座標 Z</param>
    [Command("look-at")]
    public async Task LookAt([Argument] float x, [Argument] float y, [Argument] float z)
    {
        var r = await _client.LookAtPositionAsync(x, y, z);
        Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
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
