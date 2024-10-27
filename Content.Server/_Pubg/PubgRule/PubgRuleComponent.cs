using Content.Shared.Roles;
using Content.Shared.Storage;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Pubg.PubgRule;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, Access(typeof(PubgRuleSystem))]
public sealed partial class PubgRuleComponent : Component
{
    /// <summary>
    /// How long until the round restarts
    /// </summary>
    [DataField("restartDelay"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RestartDelay = TimeSpan.FromMinutes(2);

    /// <summary>
    /// The person who won.
    /// We store this here in case of some assist shenanigans.
    /// </summary>
    [DataField("victor")]
    public NetUserId? Victor;

    /// <summary>
    /// An entity spawned after a player is killed.
    /// </summary>
    [DataField("rewardSpawns")]
    public List<EntitySpawnEntry> RewardSpawns = new();

    /// <summary>
    /// The gear all players spawn with.
    /// </summary>
    [DataField("gear", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Gear = "PubgGear";

    [DataField]
    public HashSet<EntityUid> Survivors = new HashSet<EntityUid>();

    [DataField]
    public Dictionary<int, string> ShopUpgrades = new Dictionary<int, string>(){
        { 10, "GoidaT2Shop" },
        { 20, "GoidaT3Shop" }
    };
}
