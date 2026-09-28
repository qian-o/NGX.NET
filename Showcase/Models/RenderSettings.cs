using Streamline.NET;

namespace Showcase.Models;

internal sealed record RenderSettings
{
    public DLSSMode Quality = DLSSMode.MaxQuality;
    public bool RayReconstruction;
    public bool FrameGeneration;

    // RR selects reconstruction only; hardware ray tracing is a renderer capability
    // and continues when this setting is off. RR without upscaling runs natively.
    public Reconstruction Reconstruction => RayReconstruction ? Reconstruction.RayReconstruction : Quality == DLSSMode.Off ? Reconstruction.Native : Reconstruction.DLSS;

    public DLSSMode ReconstructionQuality => Quality == DLSSMode.Off && RayReconstruction ? DLSSMode.DLAA : Quality;

    public void Reset(RenderCapabilities capabilities)
    {
        Quality = capabilities.Dlss ? DLSSMode.MaxQuality : DLSSMode.Off;
        RayReconstruction = capabilities.RayReconstruction;
        FrameGeneration = capabilities.FrameGeneration;
    }
}
