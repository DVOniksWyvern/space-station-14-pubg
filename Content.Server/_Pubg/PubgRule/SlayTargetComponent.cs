namespace Content.Server._Pubg.PubgRule;

[RegisterComponent]
public sealed partial class SlayTargetComponent : Component
{
    [DataField]
    public EntityUid? SlayBoundary = null;

    [DataField]
    public EntityUid? ScreamAction;

    [DataField]
    public EntityUid? Shop;
}
