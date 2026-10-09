namespace Showcase.Models;

internal readonly struct RenderCapabilities(bool dlss, bool rayReconstruction, bool frameGeneration)
{
    public readonly bool Dlss = dlss;

    public readonly bool RayReconstruction = rayReconstruction;

    public readonly bool FrameGeneration = frameGeneration;
}
