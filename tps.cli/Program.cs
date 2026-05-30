using System.CommandLine;
using System.Text.Json;
using Cysharp.AI;
using tps.client;

var http = new HttpClient();
var client = new GameApiClient(http);

var root = new RootCommand("tps-cli: コマンドラインからゲームを操作する");

// ping
{
    var cmd = new Command("ping", "ゲームサーバーの疎通確認");
    cmd.SetAction(async (_, ct) =>
    {
        try
        {
            var r = await client.PingAsync();
            Console.WriteLine(r?.Message ?? "no response");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"unreachable: {ex.Message}");
            return 1;
        }
        return 0;
    });
    root.Add(cmd);
}

// state
{
    var cmd = new Command("state", "ゲームの現在状態を取得");
    cmd.SetAction(async (_, ct) =>
    {
        try
        {
            var r = await client.GetStateAsync();
            Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        return 0;
    });
    root.Add(cmd);
}

// commands
{
    var cmd = new Command("commands", "現在シーンで使えるコマンド一覧を取得");
    cmd.SetAction(async (_, ct) =>
    {
        try
        {
            var r = await client.GetAvailableCommandsAsync();
            Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        return 0;
    });
    root.Add(cmd);
}

// actions
{
    var cmd = new Command("actions", "InputMap のアクション名一覧を取得");
    cmd.SetAction(async (_, ct) =>
    {
        try
        {
            var r = await client.GetActionsAsync();
            foreach (var a in r?.Actions ?? [])
                Console.WriteLine(a);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        return 0;
    });
    root.Add(cmd);
}

// press
{
    var actionArg = new Argument<string>("action") { Description = "InputMap アクション名 (例: move_forward)" };
    var msOpt = new Option<int>("--ms") { Description = "ボタンを押す時間 (ms)", DefaultValueFactory = _ => 100 };
    var cmd = new Command("press", "InputMap アクションを押す") { actionArg, msOpt };
    cmd.SetAction(async (parseResult, ct) =>
    {
        var action = parseResult.GetValue(actionArg)!;
        var ms = parseResult.GetValue(msOpt);
        try
        {
            var r = await client.PressActionAsync(action, ms);
            if (r is null) { Console.Error.WriteLine("error: no response"); return 1; }
            Console.WriteLine(r.Success ? $"ok: {r.Message}" : $"error: {r.Message}");
            return r.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    });
    root.Add(cmd);
}

// aim
{
    var valueArg = new Argument<bool>("value") { Description = "true=ADS ON / false=ADS OFF" };
    var cmd = new Command("aim", "ADS (照準) 状態を設定") { valueArg };
    cmd.SetAction(async (parseResult, ct) =>
    {
        var value = parseResult.GetValue(valueArg);
        try
        {
            var r = await client.SetAimingAsync(value);
            if (r is null) { Console.Error.WriteLine("error: no response"); return 1; }
            Console.WriteLine(r.Success ? $"ok: {r.Message}" : $"error: {r.Message}");
            return r.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    });
    root.Add(cmd);
}

// pitch
{
    var degreesArg = new Argument<float>("degrees") { Description = "ピッチ角度 (上=正 / 下=負, 範囲: -68.8 〜 45.8)" };
    var cmd = new Command("pitch", "カメラのピッチ角を設定") { degreesArg };
    cmd.SetAction(async (parseResult, ct) =>
    {
        var degrees = parseResult.GetValue(degreesArg);
        try
        {
            var r = await client.SetCameraPitchAsync(degrees);
            Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        return 0;
    });
    root.Add(cmd);
}

// look-at
{
    var xArg = new Argument<float>("x") { Description = "ワールド座標 X" };
    var yArg = new Argument<float>("y") { Description = "ワールド座標 Y" };
    var zArg = new Argument<float>("z") { Description = "ワールド座標 Z" };
    var cmd = new Command("look-at", "指定ワールド座標の方向へカメラを向ける") { xArg, yArg, zArg };
    cmd.SetAction(async (parseResult, ct) =>
    {
        var x = parseResult.GetValue(xArg);
        var y = parseResult.GetValue(yArg);
        var z = parseResult.GetValue(zArg);
        try
        {
            var r = await client.LookAtPositionAsync(x, y, z);
            Console.WriteLine(ToonEncoder.Encode(JsonSerializer.SerializeToElement(r)));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        return 0;
    });
    root.Add(cmd);
}

// screenshot
{
    var outputOpt = new Option<string>("--output") { Description = "保存先ファイルパス", DefaultValueFactory = _ => "screenshot.png" };
    var cmd = new Command("screenshot", "ゲームのスクリーンショットを撮る") { outputOpt };
    cmd.SetAction(async (parseResult, ct) =>
    {
        var output = parseResult.GetValue(outputOpt)!;
        try
        {
            var bytes = await client.TakeScreenshotAsync();
            await File.WriteAllBytesAsync(output, bytes);
            Console.WriteLine($"saved: {output} ({bytes.Length} bytes)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        return 0;
    });
    root.Add(cmd);
}

// help
{
    var subArg = new Argument<string?>("command") { Description = "詳細を見るコマンド名", Arity = ArgumentArity.ZeroOrOne };
    var cmd = new Command("help", "使い方を表示する (help <command> で個別コマンドの詳細)") { subArg };
    cmd.SetAction((parseResult, ct) =>
    {
        var sub = parseResult.GetValue(subArg);
        var target = sub is null ? "--help" : $"{sub} --help";
        return root.Parse(target).InvokeAsync();
    });
    root.Add(cmd);
}

return await root.Parse(args).InvokeAsync();
