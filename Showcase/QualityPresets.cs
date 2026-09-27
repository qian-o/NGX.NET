using Streamline.NET;

namespace Showcase;

internal enum QualityPreset
{
    Recommended, Quality, Performance, Native
}

internal readonly record struct PresetCapabilities(bool Dlss, bool RayReconstruction, bool FrameGeneration, bool Reflex);

internal static class QualityPresets
{
    public static void Apply(RenderSettings settings, QualityPreset preset, PresetCapabilities capabilities)
    {
        settings.RayTracing = capabilities.RayReconstruction && preset is QualityPreset.Recommended or QualityPreset.Quality;
        settings.Reconstruction = settings.RayTracing ? Reconstruction.RayReconstruction :
            preset == QualityPreset.Native || !capabilities.Dlss ? Reconstruction.Native :
            preset == QualityPreset.Quality ? Reconstruction.DLAA : Reconstruction.DLSS;
        settings.Quality = preset switch
        {
            QualityPreset.Quality => DLSSMode.DLAA,
            QualityPreset.Performance => DLSSMode.Balanced,
            _ => DLSSMode.MaxQuality
        };
        settings.GeneratedFrames = preset != QualityPreset.Native && capabilities.FrameGeneration ? 1u : 0;
        settings.Reflex = capabilities.Reflex ? ReflexMode.LowLatency : ReflexMode.Off;
        settings.DeepDVC = false;
    }

    public static QualityPreset? Match(RenderSettings settings, PresetCapabilities capabilities)
    {
        foreach (QualityPreset preset in Enum.GetValues<QualityPreset>())
        {
            RenderSettings expected = new();
            Apply(expected, preset, capabilities);
            if (settings.Reconstruction == expected.Reconstruction && settings.Quality == expected.Quality &&
                settings.RayTracing == expected.RayTracing && settings.GeneratedFrames == expected.GeneratedFrames &&
                settings.Reflex == expected.Reflex && settings.DeepDVC == expected.DeepDVC)
            {
                return preset;
            }
        }
        return null;
    }

    public static void ResetDaylight(RenderSettings settings)
    {
        RenderSettings defaults = new();
        settings.Exposure = defaults.Exposure;
        settings.SunElevation = defaults.SunElevation;
        settings.SunAzimuth = defaults.SunAzimuth;
        settings.SunIntensity = defaults.SunIntensity;
        settings.SkyIntensity = defaults.SkyIntensity;
        settings.LocalLightIntensity = defaults.LocalLightIntensity;
        settings.ContactShadows = defaults.ContactShadows;
        settings.DeepDVC = false;
    }
}
