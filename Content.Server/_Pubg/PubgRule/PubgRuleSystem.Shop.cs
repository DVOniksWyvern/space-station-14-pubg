namespace Content.Server._Pubg.PubgRule;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class PubgRuleSystem
{
    public EntityUid? GetShop(EntityUid uid)
    {
        if (!TryComp<SlayTargetComponent>(uid, out var comp))
            return null;

        return comp.Shop;
    }
}
