using System.Numerics;
using Content.Client.Parallax;
using Content.Shared._Pubg;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Pubg;

public sealed partial class SlayBoundaryOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    private readonly ParallaxSystem _parallax;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private IRenderTexture? _blep;

    private readonly ShaderInstance _shader;

    public SlayBoundaryOverlay(ParallaxSystem parallax, SpriteSystem sprite)
    {
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        _parallax = parallax;
        _sprite = sprite;
        IoCManager.InjectDependencies(this);
        _shader = _protoManager.Index<ShaderPrototype>("WorldGradientCircle").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mapUid = _mapManager.GetMapEntityId(args.MapId);
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        if (_entManager.TryGetComponent<SlayRestrictedRangeComponent>(mapUid, out var range))
        {
            DrawSlayBoundary(args, range, invMatrix);
        }
    }

    private void DrawSlayBoundary(in OverlayDrawArgs args, SlayRestrictedRangeComponent rangeComp, Matrix3x2 invMatrix)
    {
        if (_blep?.Texture.Size != args.Viewport.Size)
        {
            _blep?.Dispose();
            _blep = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather-stencil");
        }

        var worldHandle = args.WorldHandle;
        var renderScale = args.Viewport.RenderScale.X;
        var zoom = args.Viewport.Eye?.Zoom ?? Vector2.One;
        var length = zoom.X;
        var bufferRange = MathF.Min(10f, rangeComp.Range);

        var pixelCenter = Vector2.Transform(rangeComp.Origin, invMatrix);
        // Something something offset?
        var vertical = args.Viewport.Size.Y;

        var pixelMaxRange = rangeComp.Range * renderScale / length * EyeManager.PixelsPerMeter;
        var pixelBufferRange = bufferRange * renderScale / length * EyeManager.PixelsPerMeter;
        var pixelMinRange = pixelMaxRange - pixelBufferRange;

        _shader.SetParameter("position", new Vector2(pixelCenter.X, vertical - pixelCenter.Y));
        _shader.SetParameter("maxRange", pixelMaxRange);
        _shader.SetParameter("minRange", pixelMinRange);
        _shader.SetParameter("bufferRange", pixelBufferRange);
        _shader.SetParameter("gradient", 0.80f);

        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;
        var localAABB = invMatrix.TransformBox(worldAABB);

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        worldHandle.RenderInRenderTarget(_blep!, () =>
        {
            worldHandle.UseShader(_shader);
            worldHandle.DrawRect(localAABB, Color.White);
        }, Color.Transparent);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_blep!.Texture, worldBounds);
        var curTime = _timing.RealTime;
        var sprite = _sprite.GetFrame(new SpriteSpecifier.Texture(new ResPath("/Textures/Parallaxes/noise.png")), curTime);

        // Draw the rain
        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilDraw").Instance());
        _parallax.DrawParallax(worldHandle, worldAABB, sprite, curTime, position, new Vector2(0.5f, 0f));
    }
}

