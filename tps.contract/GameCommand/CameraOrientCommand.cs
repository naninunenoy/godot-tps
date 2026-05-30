using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct CameraOrientCommand : ICommand
{
    public float YawDelta;
    public float Pitch;
}
