using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Hands.Systems;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Points;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Pubg.PubgRule;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class PubgRuleSystem : GameRuleSystem<PubgRuleComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PointSystem _point = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        // SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<SlayTargetComponent, MobStateChangedEvent>(OnMobStateChanged);
        //SubscribeLocalEvent<PubgRuleComponent, PlayerPointChangedEvent>(OnPointChanged);
        //SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = EntityQueryEnumerator<PubgRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var pubg, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var newMind = _mind.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mind.SetUserId(newMind, ev.Player.UserId);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);
            SetOutfitCommand.SetOutfit(mob, pubg.Gear, EntityManager);
            EnsureComp<KillTrackerComponent>(mob);

            _point.EnsurePlayer(ev.Player.UserId, uid, point);

            if (ev.Player.AttachedEntity != null)
            {
                AddComp<SlayTargetComponent>(ev.Player.AttachedEntity.Value);
                pubg.Survivors.Add(ev.Player.AttachedEntity.Value);
            }

            //var shop = Spawn("GoidaShop");

            //_inventory.TryEquip(ev.Player.AttachedEntity.Value, shop, "pocket1");
            //_hands.
            //TODO: spawn shop in pocket here or link with existing shop here


            ev.Handled = true;
            break;
        }
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<KillTrackerComponent>(ev.Mob);
        var query = EntityQueryEnumerator<PubgRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;
            //_respawn.AddToTracker(ev.Mob, uid, tracker);
        }
    }

    private void OnMobStateChanged(EntityUid uid, SlayTargetComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        if (!TryComp<ActorComponent>(ev.Target, out var actor))
            return;

        var query = EntityQueryEnumerator<PubgRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleUid, out var pubg, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(ruleUid, rule))
                continue;

            pubg.Survivors.Remove(uid);

            if (pubg.Survivors.Count == 1)
            {
                if (TryComp<ActorComponent>(pubg.Survivors.Last(), out var victor))
                    pubg.Victor = victor.PlayerSession.UserId;

                _roundEnd.EndRound(pubg.RestartDelay);
            }
        }
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<PubgRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var pubg, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (ev.Primary is not KillPlayerSource player)
            {
                _point.AdjustPointValue(ev.Entity, -1, uid, point);
                continue;
            }

            _point.AdjustPointValue(player.PlayerId, 1, uid, point);

            if (ev.Assist is KillPlayerSource assist && pubg.Victor == null)
                _point.AdjustPointValue(assist.PlayerId, 1, uid, point);

            var spawns = EntitySpawnCollection.GetSpawns(pubg.RewardSpawns).Cast<string?>().ToList();
            EntityManager.SpawnEntities(Transform(ev.Entity).MapPosition, spawns);

            // upgrade pubgShop

            if (!_player.TryGetSessionById(player.PlayerId, out var session))
                return;

            if (session.AttachedEntity == null)
                return;

            var Puid = session.AttachedEntity.Value;
            if (!TryComp<SlayTargetComponent>(uid, out var comp))
                return;

            if (comp.Shop == null)
                return;

            var shop = GetShop(Puid);

            if (shop == null)
                return;

            var kills = _point.GetPointValue(player.PlayerId, Puid);

            foreach (var upgrade in pubg.ShopUpgrades)
            {
                if (!(upgrade.Key <= kills))
                    continue;

                _tag.AddTag(shop.Value, upgrade.Value);
            }
        }
    }

    private void OnPointChanged(EntityUid uid, PubgRuleComponent component, ref PlayerPointChangedEvent args)
    {
        //if (component.Victor != null)
        //    return;

        //if (args.Points < component.KillCap)
        //    return;

        //component.Victor = args.Player;
        //_roundEnd.EndRound(component.RestartDelay);
    }

    // private void OnRoundEndText(ref RoundEndTextAppendEvent ev)
    // {
    //     var query = EntityQueryEnumerator<PubgRuleComponent, PointManagerComponent, GameRuleComponent>();
    //     while (query.MoveNext(out var uid, out var dm, out var point, out var rule))
    //     {
    //         if (!GameTicker.IsGameRuleAdded(uid, rule))
    //             continue;
    //
    //         if (dm.Victor != null && _player.TryGetPlayerData(dm.Victor.Value, out var data))
    //         {
    //             ev.AddLine(Loc.GetString("point-scoreboard-winner", ("player", data.UserName)));
    //             ev.AddLine("");
    //         }
    //         ev.AddLine(Loc.GetString("point-scoreboard-header"));
    //         ev.AddLine(new FormattedMessage(point.Scoreboard).ToMarkup());
    //     }
    // }
}
