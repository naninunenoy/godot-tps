using tps.contract;

namespace tps.csharp;

public sealed class InGameScene : IScene
{
    public IReadOnlyList<ICommandDescriptor> AvailableCommands =>
        [CommandDescriptor.Of<GamePauseRequestedCommand>()];
}
