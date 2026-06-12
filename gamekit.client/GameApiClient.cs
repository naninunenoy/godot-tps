using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using gamekit.contract.Mcp;

namespace gamekit.client;

public class GameApiClient
{
    protected HttpClient Http { get; }

    public GameApiClient(HttpClient http)
    {
        Http = http;
        Http.BaseAddress = new Uri(InputEndpoints.BaseUrl);
        Http.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<PingResponse?> PingAsync()
    {
        var response = await Http.GetAsync(InputEndpoints.Ping);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PingResponse>();
    }

    public async Task<GetActionsResponse?> GetActionsAsync()
    {
        var response = await Http.GetAsync(InputEndpoints.Actions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetActionsResponse>();
    }

    public async Task<byte[]> TakeScreenshotAsync()
    {
        var response = await Http.GetAsync(InputEndpoints.Screenshot);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<PressActionResponse?> PressActionAsync(string action, int durationMs)
    {
        var content = Serialize(new PressActionRequest(action, durationMs));
        var response = await Http.PostAsync(InputEndpoints.PressAction, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PressActionResponse>();
    }

    public async Task<CommandListResponse?> GetAvailableCommandsAsync()
    {
        var response = await Http.GetAsync(InputEndpoints.Commands);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandListResponse>();
    }

    /// <summary>/state のペイロード型はゲーム定義のため、型引数でゲーム側が指定する。</summary>
    public async Task<TState?> GetStateAsync<TState>()
    {
        var response = await Http.GetAsync(InputEndpoints.State);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TState>();
    }

    /// <summary>/state の素の JSON を返す。型を介さず中継する用途（MCP のエンコード変換など）。</summary>
    public async Task<string> GetStateRawAsync()
    {
        var response = await Http.GetAsync(InputEndpoints.State);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    protected static StringContent Serialize<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
