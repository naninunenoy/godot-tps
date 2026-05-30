using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct AmmoChangedCommand : ICommand
{
    public int CurrentAmmo;
    public int MagazineSize;
    public bool IsReloading;
}
