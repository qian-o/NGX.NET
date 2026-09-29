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

    public PerfQualityValue ReconstructionQuality => Quality switch
    {
        QualityMode.Off => PerfQualityValue.DLAA,
        QualityMode.MaxQuality => PerfQualityValue.MaxQuality,
        QualityMode.Balanced => PerfQualityValue.Balanced,
        QualityMode.MaxPerformance => PerfQualityValue.MaxPerf,
        QualityMode.UltraPerformance => PerfQualityValue.UltraPerformance,
        _ => throw new ArgumentOutOfRangeException()
    };

    public void Reset(RenderCapabilities capabilities)
    {
        Quality = capabilities.Dlss ? QualityMode.MaxQuality : QualityMode.Off;
        RayReconstruction = capabilities.RayReconstruction;
        FrameGeneration = capabilities.FrameGeneration;
    }
}
