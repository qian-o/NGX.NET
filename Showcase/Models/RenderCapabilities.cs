namespace Showcase.Models;

internal readonly struct RenderCapabilities(bool dlss, bool rayReconstruction, bool frameGeneration)
{
    public readonly bool DlssSupported = dlss;

    public readonly bool RayReconstructionSupported = rayReconstruction;

    public readonly bool FrameGenerationSupported = frameGeneration;
}
