using VitalRouter;

namespace tps.contract.Mcp;

/// <summary>HTTP リクエスト DTO 兼コマンド。受信後そのまま Router へ publish される。</summary>
public record SetAimingRequest(bool IsAiming) : ICommand;
