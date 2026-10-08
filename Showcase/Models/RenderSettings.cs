using NGX.NET;

namespace Showcase.Models;

internal sealed record RenderSettings
{
    public QualityMode Quality = QualityMode.MaxQuality;

    public bool RayReconstruction;

    public bool FrameGeneration;

    // RR selects reconstruction only; hardware ray tracing is a renderer capability
    // and continues when this setting is off. RR without upscaling runs natively.
    public Reconstruction Reconstruction => RayReconstruction ? Reconstruction.RayReconstruction : Quality == QualityMode.Off ? Reconstruction.Native : Reconstruction.DLSS;

    public NGXPerfQualityValue ReconstructionQuality => Quality switch
    {
        QualityMode.Off => NGXPerfQualityValue.Dlaa,
        QualityMode.MaxQuality => NGXPerfQualityValue.MaxQuality,
        QualityMode.Balanced => NGXPerfQualityValue.Balanced,
        QualityMode.MaxPerformance => NGXPerfQualityValue.MaxPerf,
        QualityMode.UltraPerformance => NGXPerfQualityValue.UltraPerformance,
        _ => throw new ArgumentOutOfRangeException()
    };

    public void Reset(RenderCapabilities capabilities)
    {
        Quality = capabilities.Dlss ? QualityMode.MaxQuality : QualityMode.Off;
        RayReconstruction = capabilities.RayReconstruction;
        FrameGeneration = capabilities.FrameGeneration;
    }
}
