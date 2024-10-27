using Content.Shared._Pubg.Components;
using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Rounding;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Pubg.Systems;

// No more dreams..
public abstract class SharedEnergySystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EnergyMoverComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EnergyMoverComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<EnergyMoverComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, EnergyMoverComponent component, ComponentStartup args)
    {
        SetEnergyAlert(uid, component);
    }

    private void OnRejuvenate(EntityUid uid, EnergyMoverComponent component, RejuvenateEvent args)
    {
        component.CurrentEnergy = component.MaxEnergy;
        SetEnergyAlert(uid, component);
        Dirty(uid, component);
    }

    private void OnShutdown(EntityUid uid, EnergyMoverComponent component, ComponentShutdown args)
    {
        SetEnergyAlert(uid);
    }

    protected void SetEnergyAlert(EntityUid uid, EnergyMoverComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
        {
            if (component != null)
                _alerts.ClearAlert(uid, component.EnergyAlert);
            return;
        }

        var severity =
            ContentHelpers.RoundToLevels(component.MaxEnergy - component.CurrentEnergy, component.MaxEnergy, 11);
        _alerts.ShowAlert(uid, component.EnergyAlert, (short) severity);
    }

    public void ChangeEnergyLevel(EntityUid uid, int amount, EnergyMoverComponent? component = null,
        InputMoverComponent? inputMover = null)
    {
        if (!Resolve(uid, ref component, false) || !Resolve(uid, ref inputMover, false) || component.Deleted)
        {
            return;
        }

        component.CurrentEnergy = Math.Clamp(amount, 0, component.MaxEnergy);
        if (component.CurrentEnergy == 0)
        {
            component.DepletionTime = Timing.CurTime;
            inputMover.HeldMoveButtons ^= MoveButtons.Walk;
        }

        SetEnergyAlert(uid, component);
        Dirty(uid, component);
    }
}
