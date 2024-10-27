using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Pubg.Components;

// White Dream Pubg...? Snob..
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EnergyMoverComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public int MaxEnergy = 100;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public int EnergyRegenerationRate = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public int EnergyConsumtionRate = 4;

    [ViewVariables, AutoNetworkedField]
    public int CurrentEnergy = 100;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EnergyRegenerationDelay = TimeSpan.FromSeconds(10);

    public TimeSpan DepletionTime;

    #region Update

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    #endregion

    [DataField]
    public ProtoId<AlertPrototype> EnergyAlert = "Energy";
}
