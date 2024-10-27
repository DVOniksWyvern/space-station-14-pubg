using System.ComponentModel.Design;
using System.Numerics;
using Content.Shared._Pubg;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Content.Shared.Salvage;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Pubg.SlayBoundary;

public sealed class SlayRestrictedRangeSystem : SharedRestrictedRangeSystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlayRestrictedRangeComponent, MapInitEvent>(OnRestrictedMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlayRestrictedRangeComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.NextUpdate)
            {
                comp.NextUpdate = _timing.CurTime + comp.dT;
                NarrowBoundary(comp.BoundaryEntity, comp, uid);
            }
        }
    }

    private void OnRestrictedMapInit(EntityUid uid, SlayRestrictedRangeComponent component, MapInitEvent args)
    {
        component.BoundaryEntity = CreateBoundary(new EntityCoordinates(uid, component.Origin), component);
    }

    public EntityUid CreateBoundary(EntityCoordinates coordinates, SlayRestrictedRangeComponent comp)
    {
        string? boundaryProtoId = null;
        if (comp.BoundaryProto != null)
        {
            _proto.TryIndex(comp.BoundaryProto, out var boundaryProto);
            boundaryProtoId = boundaryProto?.ID;
        }

        var boundaryUid = Spawn(boundaryProtoId, coordinates);

        if (boundaryProtoId == null)
            AddComp<SlayBoundaryComponent>(boundaryUid);

        CreateSlayBoundary(boundaryUid, comp.Range);

        return boundaryUid;
    }

    public void CreateSlayBoundary(EntityUid uid, float range)
    {
        if (!TryComp<PhysicsComponent>(uid, out var boundaryPhysics))
            boundaryPhysics = AddComp<PhysicsComponent>(uid);

        var boundary = new PhysShapeCircle(range + 0.25f, Vector2.Zero);

        _fixtures.TryCreateFixture(
            uid,
            boundary,
            "slay_boundary",
            1f,
            false,
            collisionLayer: (int) (CollisionGroup.HighImpassable | CollisionGroup.Impassable | CollisionGroup.LowImpassable),
            body: boundaryPhysics);

        _physics.WakeBody(uid, body: boundaryPhysics);
    }

    //TODO: сделать объявление через AnnouncementSystem, добавить делегат и кастомные функции для этой хуйни
    public void NarrowBoundary(EntityUid boundaryUid, SlayRestrictedRangeComponent comp, EntityUid uid)
    {
        if (!TryComp<FixturesComponent>(boundaryUid, out var fixturesManager))
            return;

        if (comp.Range < 10)
            return;

        comp.Range -= comp.dR;

        if (!fixturesManager.Fixtures.TryGetValue("slay_boundary", out var fixture))
            return;

        _physics.SetRadius(boundaryUid, "slay_boundary", fixture, fixture.Shape, comp.Range);

        Dirty(uid, comp);
    }

    public void Update(EntityUid uid, SlayRestrictedRangeComponent comp)
    {
        if (!TryComp<FixturesComponent>(comp.BoundaryEntity, out var fixturesManager))
            return;

        if (!fixturesManager.Fixtures.TryGetValue("slay_boundary", out var fixture))
            return;

        if (float.Round(comp.Range) != float.Round(fixture.Shape.Radius))
        {
            _physics.SetRadius(comp.BoundaryEntity, "slay_boundary", fixture, fixture.Shape, fixture.Shape.Radius - comp.dR);
        }

        Dirty(uid, comp);
    }
}
