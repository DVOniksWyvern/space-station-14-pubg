using Content.Shared._Pubg.Components;
using Content.Shared._Pubg.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server._Pubg.Systems;

public sealed class EnergySystem : SharedEnergySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InputMoverComponent, EnergyMoverComponent>();
        var currentTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var inputMover, out var energy))
        {
            if (currentTime < energy.NextUpdateTime)
            {
                continue;
            }

            energy.NextUpdateTime += energy.UpdateRate;

            if (inputMover.Sprinting)
            {
                ChangeEnergyLevel(uid, energy.CurrentEnergy - energy.EnergyConsumtionRate, energy);
                continue;
            }

            if (energy.CurrentEnergy == energy.MaxEnergy ||
                currentTime < energy.DepletionTime + energy.EnergyRegenerationDelay)
            {
                continue;
            }

            ChangeEnergyLevel(uid, energy.CurrentEnergy + energy.EnergyRegenerationRate, energy);
        }
    }
}
