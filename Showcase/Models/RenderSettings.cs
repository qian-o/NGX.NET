using NGX.NET;

namespace Showcase.Models;

internal struct RenderSettings
{
    public QualityMode Quality = QualityMode.MaxQuality;

    public bool RayReconstruction;

    public bool FrameGeneration;

    public RenderSettings()
    {
    }

    // Hardware ray tracing remains active when ray reconstruction is disabled.
    public Reconstruction Reconstruction => (RayReconstruction, Quality) switch
    {
        (true, _) => Reconstruction.RayReconstruction,
        (_, QualityMode.Off) => Reconstruction.Native,
        _ => Reconstruction.DLSS
    };

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
