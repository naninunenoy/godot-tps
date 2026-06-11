using gamekit.contract.GameCommand;
using tps.contract.Mcp;

namespace tps.csharp;

public sealed class InGameScene : IScene
{
    public IReadOnlyList<ICommandDescriptor> AvailableCommands =>
        [
            CommandDescriptor.Of<GamePauseRequestedCommand>(),
            CommandDescriptor.Of<SetCameraPitchRequest>(),
            CommandDescriptor.Of<LookAtPositionRequest>(),
        ];
}
