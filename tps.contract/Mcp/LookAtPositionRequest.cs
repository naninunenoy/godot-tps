using VitalRouter;

namespace tps.contract.Mcp;

/// <summary>HTTP リクエスト DTO 兼コマンド。受信後そのまま Router へ publish される。</summary>
public record LookAtPositionRequest(float X, float Y, float Z) : ICommand;
