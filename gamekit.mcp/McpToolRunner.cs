using System.Text.Json;
using Cysharp.AI;

namespace gamekit.mcp;

/// <summary>
/// MCP ツール実装の定型処理。通信失敗の "unreachable: ..." 整形と
/// ToonEncoder 変換を 1 箇所に集約する（gamekit.mcp / ゲーム側ツール共用）。
/// </summary>
public static class McpToolRunner
{
    /// <summary>API 呼び出しを実行し、例外は "unreachable: ..." の文字列にして返す。</summary>
    public static async Task<string> RunAsync(Func<Task<string>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            return $"unreachable: {ex.Message}";
        }
    }

    /// <summary>API レスポンスを ToonEncoder でエンコードして返す（例外は "unreachable: ..."）。</summary>
    public static Task<string> EncodeAsync<T>(Func<Task<T>> action) =>
        RunAsync(async () =>
        {
            var payload = await action();
            return ToonEncoder.Encode(JsonSerializer.SerializeToElement(payload));
        });
}
