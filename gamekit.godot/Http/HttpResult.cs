using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gamekit.godot;

/// <summary>ルートハンドラが返す HTTP レスポンス。</summary>
public sealed record HttpResult(int Status, byte[] Body, string ContentType)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static HttpResult Json<T>(T payload, int status = 200) =>
        new(
            status,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions)),
            "application/json; charset=utf-8"
        );

    public static HttpResult Text(string body, int status = 200) =>
        new(status, Encoding.UTF8.GetBytes(body), "text/plain; charset=utf-8");

    public static HttpResult Binary(byte[] body, string contentType, int status = 200) =>
        new(status, body, contentType);
}
