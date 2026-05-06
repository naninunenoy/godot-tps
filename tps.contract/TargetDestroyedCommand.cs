using VitalRouter;

namespace tps.contract;

// [MRubyObject] を付与することで mruby スクリプトからも発行可能になる
public partial struct TargetDestroyedCommand : ICommand
{
    public string TargetName;
}
