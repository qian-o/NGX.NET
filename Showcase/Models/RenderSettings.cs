using System.Numerics;
using NGX.NET;

namespace Showcase.Models;

internal struct RenderSettings
{
    public QualityMode Quality = QualityMode.MaxQuality;

    public float DaylightProgress = 0.5f;

    public bool IsRayReconstructionEnabled;

    public bool IsFrameGenerationEnabled;

    public RenderSettings()
    {
    }

    public readonly Vector3 SunDirection
    {
        get
        {
            // Sunrise -Z, noon +Y, sunset +Z. Exact cardinal directions avoid tiny below-horizon components from floating-point sin(pi).
            float progress = Math.Clamp(DaylightProgress, 0.0f, 1.0f);
            if (progress is 0.0f)
            {
                return -Vector3.UnitZ;
            }

            if (progress is 0.5f)
            {
                return Vector3.UnitY;
            }

            if (progress is 1.0f)
            {
                return Vector3.UnitZ;
            }

            float angle = MathF.PI * progress;

            return Vector3.Normalize(new(0.0f, MathF.Sin(angle), -MathF.Cos(angle)));
        }
    }

    // Hardware ray tracing remains active when ray reconstruction is disabled.
    public readonly Reconstruction Reconstruction
    {
        get
        {
            return (IsRayReconstructionEnabled, Quality) switch
            {
                (true, _) => Reconstruction.RayReconstruction,
                (_, QualityMode.Off) => Reconstruction.Native,
                _ => Reconstruction.DLSS
            };
        }
    }

    public readonly NGXPerfQualityValue ReconstructionQuality
    {
        get
        {
            return Quality switch
            {
                QualityMode.Off => NGXPerfQualityValue.Dlaa,
                QualityMode.MaxQuality => NGXPerfQualityValue.MaxQuality,
                QualityMode.Balanced => NGXPerfQualityValue.Balanced,
                QualityMode.MaxPerformance => NGXPerfQualityValue.MaxPerf,
                QualityMode.UltraPerformance => NGXPerfQualityValue.UltraPerformance,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public void Reset(RenderCapabilities capabilities)
    {
        Quality = capabilities.DlssSupported ? QualityMode.MaxQuality : QualityMode.Off;
        IsRayReconstructionEnabled = capabilities.RayReconstructionSupported;
        IsFrameGenerationEnabled = capabilities.FrameGenerationSupported;
    }
}
