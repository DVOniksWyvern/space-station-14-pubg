using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Movement.Components;

namespace Content.Server._Pubg.SlayBoundary;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SlayBoundaryComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Offset = 2f;

    [DataField("damage")] public DamageSpecifier Damage = new()
    {
        DamageDict = new ()
        {
            { "Asphyxiation", 10 },
        }
    };
}
