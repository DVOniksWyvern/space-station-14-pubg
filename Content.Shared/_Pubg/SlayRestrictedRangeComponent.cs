using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Pubg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlayRestrictedRangeComponent : Component
{
    [DataField(required: true), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 38f;

    [DataField("dR")]
    public float dR = 0.01f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Origin;

    [DataField]
    public EntityUid BoundaryEntity;

    [DataField("boundaryProto")]
    public string? BoundaryProto;

    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField("dT")]
    public TimeSpan dT = TimeSpan.FromSeconds(1);
}
