namespace tps.csharp;

public static class GameEvents
{
    public const string ShotFired = nameof(ShotFired);
    public const string ReloadStarted = nameof(ReloadStarted);
    public const string ReloadCompleted = nameof(ReloadCompleted);
    public const string TargetHit = nameof(TargetHit);
    public const string TargetDestroyed = nameof(TargetDestroyed);
    public const string TargetRespawned = nameof(TargetRespawned);
    public const string KillCountChanged = nameof(KillCountChanged);
    public const string GamePaused = nameof(GamePaused);
    public const string GameResumed = nameof(GameResumed);
}
