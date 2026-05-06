using VitalRouter;

namespace tps.contract;

public partial struct CameraOrientCommand : ICommand
{
    public float YawDelta;
    public float Pitch;
}
