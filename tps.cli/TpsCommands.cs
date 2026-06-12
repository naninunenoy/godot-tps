using ConsoleAppFramework;
using System.Text.Json;
using Cysharp.AI;
using tps.client;

/// <summary>TPS 固有のコマンド。汎用コマンドは GameCommands を参照。</summary>
public class TpsCommands
{
    private readonly TpsGameApiClient _client = new(new HttpClient());

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
}
