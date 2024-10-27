using Content.Server.GameTicking;
using Content.Shared._Pubg.Components;

namespace Content.Server._Pubg.EntitySystems
{
    public sealed partial class LightCycleSystem : EntitySystem
    {
        [Dependency] private readonly GameTicker _gameTicker = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LightCycleComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnComponentStartup(EntityUid uid, LightCycleComponent cycle, ComponentStartup args)
        {
            cycle.Offset = _gameTicker.RoundDuration();
        }

    }

}
