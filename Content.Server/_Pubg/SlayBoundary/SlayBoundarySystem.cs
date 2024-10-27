using Content.Server._Pubg.PubgRule;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Pubg.SlayBoundary;


/// TODO: remove code duplication, add delegates for slay range ticking
public sealed class SlayBoundarySystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SlayRestrictedRangeSystem _SlayRange = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public TimeSpan _timeToKill;

    public DamageSpecifier _defaultDamage = new ()
    {
        DamageDict = new()
        {
            { "Asphyxiation", 10 },
        }
    };

    // компонент сливается клиенту, поэтому так. мб можно сделать не нетворкающимся полем в компоненте, но я за них не ебу. короче, TODO
    public Dictionary<SlayBoundaryComponent, HashSet<EntityUid>> _SlayList = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlayBoundaryComponent, StartCollideEvent>(OnBoundaryCollide);
        SubscribeLocalEvent<SlayBoundaryComponent, EndCollideEvent>(OnBoundaryCollideEnd);

        _timeToKill = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var players = new HashSet<EntityUid>();
        var query = EntityQueryEnumerator<SlayBoundaryComponent>();

        if (_timing.CurTime > _timeToKill)
        {
            _timeToKill = _timing.CurTime + TimeSpan.FromSeconds(1);
        }
        else
        {
            return;
        }

        while (query.MoveNext(out var uid, out var comp))
        {
            var tempSet = KillListing(uid, comp);
            foreach (var loh in tempSet)
            {
                players.Add(loh);
            }
        }

        // Killing those who out of boundary
        var targetQuery = EntityQueryEnumerator<SlayTargetComponent, FixturesComponent, MobStateComponent>();
        while (targetQuery.MoveNext(out var uid, out var comp, out _, out var state))
        {
            if (state.CurrentState == MobState.Dead)
                return;

            if (players.Contains(uid))
                continue;

            if (comp.SlayBoundary == null)
                continue;

            var slayFixture = _fixtures.GetFixtureOrNull(comp.SlayBoundary.Value, "slay_boundary");

            if (slayFixture == null)
                return;

            var pos = _transform.GetWorldPosition(comp.SlayBoundary.Value);
            var radius = slayFixture.Shape.Radius;

            if ((_transform.GetWorldPosition(uid) - pos).Length() < radius)
                continue;

            _damage.TryChangeDamage(uid, _defaultDamage, true);
        }
    }

    public HashSet<EntityUid> KillListing(EntityUid uid, SlayBoundaryComponent comp)
    {
        var listToSlay = new HashSet<EntityUid>();
        if (!_SlayList.TryGetValue(comp, out var targetList))
            return listToSlay;

        foreach (var player in targetList)
        {
            if (_mobState.IsDead(player))
                continue;

            if (!TryComp<DamageableComponent>(player, out var TargetDamageComp))
                continue;

            _damage.TryChangeDamage(player, comp.Damage, true);
            listToSlay.Add(player);
        }

        return listToSlay;
    }

    private void OnBoundaryCollide(Entity<SlayBoundaryComponent> target, ref StartCollideEvent args)
    {
        if (!HasComp<ActorComponent>(args.OtherEntity))
            return;

        if (!_SlayList.ContainsKey(target.Comp))
            return;

        if (!TryComp<SlayTargetComponent>(args.OtherEntity, out var lohComp))
        {
            var slayTargetAdded = AddComp<SlayTargetComponent>(args.OtherEntity);
            slayTargetAdded.SlayBoundary = target.Owner;
        }
        else
        {
            lohComp.SlayBoundary = target.Owner;
        }

        _SlayList[target.Comp].Remove(args.OtherEntity);
    }

    private void OnBoundaryCollideEnd(Entity<SlayBoundaryComponent> target, ref EndCollideEvent args)
    {
        if (!HasComp<ActorComponent>(args.OtherEntity))
            return;

        if (!_SlayList.ContainsKey(target.Comp))
            _SlayList[target.Comp] = new HashSet<EntityUid>();

        _SlayList[target.Comp].Add(args.OtherEntity);
    }
}
