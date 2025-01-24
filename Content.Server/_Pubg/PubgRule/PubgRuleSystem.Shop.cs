namespace Content.Server._Pubg.PubgRule;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class PubgRuleSystem
{
    private EntityUid? GetShop(EntityUid uid)
    {
        return !TryComp<SlayTargetComponent>(uid, out var comp) ? null : comp.Shop;
    }
}
