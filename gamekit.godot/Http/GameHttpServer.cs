using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace gamekit.godot;

/// <summary>
/// ゲーム内に立てるリモート操作用の簡易 HTTP サーバー。
/// Node ではないプレーンクラスなので、ゲーム側の Node（autoload 等）が
/// 生成して _Process から Poll() を呼ぶ。フレーム待ちは SceneTree 経由で行う。
/// </summary>
public sealed class GameHttpServer(SceneTree tree)
{
    // リクエスト解釈は大文字小文字を区別しない（クライアント実装のプロパティ命名差を許容する）。
    // レスポンス側の方針（PascalCase・null 省略）は HttpResult.Json が持つ
    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly TcpServer _tcpServer = new();
    private readonly Dictionary<(string Method, string Path), Func<string, Task<HttpResult>>> _routes =
        new();

    public void MapGet(string path, Func<Task<HttpResult>> handler) =>
        Map("GET", path, _ => handler());

    public void MapGet(string path, Func<HttpResult> handler) =>
        Map("GET", path, _ => Task.FromResult(handler()));

    public void MapPost(string path, Func<string, Task<HttpResult>> handler) =>
        Map("POST", path, handler);

    /// <summary>
    /// JSON ボディを TReq に解釈して同期ハンドラへ渡す。
    /// ボディが不正（JSON でない・null）な場合は 400 を返す。
    /// </summary>
    public void MapPostJson<TReq>(string path, Func<TReq, HttpResult> handler) =>
        Map(
            "POST",
            path,
            body =>
            {
                TReq? request;
                try
                {
                    request = JsonSerializer.Deserialize<TReq>(body, RequestJsonOptions);
                }
                catch (JsonException)
                {
                    request = default;
                }
                return Task.FromResult(
                    request is null ? HttpResult.Text("invalid request", 400) : handler(request)
                );
            }
        );

    private void Map(string method, string path, Func<string, Task<HttpResult>> handler)
    {
        // パス定数が複数クラスに分かれており重複をコンパイル時に検出できないため、登録時に検出する
        if (!_routes.TryAdd((method, path), handler))
            throw new InvalidOperationException($"Route already registered: {method} {path}");
    }

    public Error Listen(int port) => _tcpServer.Listen((ushort)port);

    public void Stop() => _tcpServer.Stop();

    /// <summary>毎フレーム呼ぶこと（Node の _Process から）。</summary>
    public void Poll()
    {
        if (_tcpServer.IsConnectionAvailable())
            HandleConnectionAsync(_tcpServer.TakeConnection());
    }

    // async void のため、ここから例外を漏らすとプロセスごと落ちる。全経路を捕捉すること
    private async void HandleConnectionAsync(StreamPeerTcp peer)
    {
        var responseSent = false;
        try
        {
            var (method, path, body) = await ReadRequestAsync(peer);
            GD.PrintRich($"[GameHttpServer] {method} {path}");

            var result = _routes.TryGetValue((method, path), out var handler)
                ? await handler(body)
                : HttpResult.Text($"not found: {path}", 404);

            WriteResponse(peer, result);
            responseSent = true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GameHttpServer] {ex.Message}");
            if (!responseSent)
            {
                var status = ex is HttpBadRequestException ? 400 : 500;
                try { WriteResponse(peer, HttpResult.Text(ex.Message, status)); }
                catch { /* peer may already be closed */ }
            }
        }
        finally
        {
            try { peer.DisconnectFromHost(); }
            catch { /* peer may already be closed */ }
        }
    }

    private async Task<(string method, string path, string body)> ReadRequestAsync(
        StreamPeerTcp peer
    )
    {
        var rawBytes = new List<byte>();
        var headerEnd = -1;

        while (headerEnd < 0)
        {
            peer.Poll();

            var status = peer.GetStatus();
            if (status is StreamPeerTcp.Status.None or StreamPeerTcp.Status.Error)
                throw new System.IO.IOException($"Connection dropped (status={status})");

            var available = peer.GetAvailableBytes();
            if (available > 0)
            {
                var result = peer.GetData(available);
                if (result[0].As<Error>() == Error.Ok)
                    rawBytes.AddRange(result[1].AsByteArray());

                for (var i = 0; i <= rawBytes.Count - 4; i++)
                {
                    if (
                        rawBytes[i] == '\r'
                        && rawBytes[i + 1] == '\n'
                        && rawBytes[i + 2] == '\r'
                        && rawBytes[i + 3] == '\n'
                    )
                    {
                        headerEnd = i;
                        break;
                    }
                }
            }

            if (headerEnd < 0)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        var headerStr = Encoding.UTF8.GetString(rawBytes.Take(headerEnd).ToArray());
        var lines = headerStr.Split("\r\n");
        var requestParts = lines[0].Split(' ');
        if (requestParts.Length < 2)
            throw new HttpBadRequestException($"malformed request line: {lines[0]}");
        var method = requestParts[0];
        var path = requestParts[1];

        var contentLength = 0;
        foreach (var line in lines.Skip(1))
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line.Split(':')[1].Trim();
                if (!int.TryParse(value, out contentLength) || contentLength < 0)
                    throw new HttpBadRequestException($"invalid Content-Length: {value}");
            }
        }

        var bodyBytes = rawBytes.Skip(headerEnd + 4).ToList();
        while (bodyBytes.Count < contentLength)
        {
            peer.Poll();

            var status = peer.GetStatus();
            if (status is StreamPeerTcp.Status.None or StreamPeerTcp.Status.Error)
                throw new System.IO.IOException($"Connection dropped (status={status})");

            var available = peer.GetAvailableBytes();
            if (available > 0)
            {
                var result = peer.GetData(available);
                if (result[0].As<Error>() == Error.Ok)
                    bodyBytes.AddRange(result[1].AsByteArray());
            }
            else
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }

        return (method, path, Encoding.UTF8.GetString(bodyBytes.Take(contentLength).ToArray()));
    }

    private static void WriteResponse(StreamPeerTcp peer, HttpResult result)
    {
        var statusText = result.Status switch
        {
            200 => "OK",
            400 => "Bad Request",
            404 => "Not Found",
            503 => "Service Unavailable",
            _ => "Error",
        };
        var header =
            $"HTTP/1.1 {result.Status} {statusText}\r\nContent-Type: {result.ContentType}\r\nContent-Length: {result.Body.Length}\r\nConnection: close\r\n\r\n";
        peer.PutData(Encoding.UTF8.GetBytes(header).Concat(result.Body).ToArray());
    }
}
