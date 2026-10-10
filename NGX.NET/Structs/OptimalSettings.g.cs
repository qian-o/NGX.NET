#nullable enable

namespace NGX.NET;

public readonly struct OptimalSettings(uint renderOptimalWidth, uint renderOptimalHeight, uint renderMaxWidth, uint renderMaxHeight, uint renderMinWidth, uint renderMinHeight, float sharpness)
{
    public uint RenderOptimalWidth { get; } = renderOptimalWidth;

    public uint RenderOptimalHeight { get; } = renderOptimalHeight;

    public uint RenderMaxWidth { get; } = renderMaxWidth;

    public uint RenderMaxHeight { get; } = renderMaxHeight;

    public uint RenderMinWidth { get; } = renderMinWidth;

    public uint RenderMinHeight { get; } = renderMinHeight;

    public float Sharpness { get; } = sharpness;
}
