using Content.Client.Parallax;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Pubg;

public sealed class SlayBoundaryOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly ParallaxSystem _parallax = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new SlayBoundaryOverlay(_parallax, _sprite));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<SlayBoundaryOverlay>();
    }
}
