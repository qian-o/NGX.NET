namespace Streamline.NET;

/// <summary>
/// Selects the runtime features to prepare and load.
/// </summary>
public struct RuntimeOptions
{
    public RenderAPI RenderAPI { get; set; }

    public bool DLSS { get; set; }

    public bool DLSSD { get; set; }

    public bool DLSSG { get; set; }

    public bool Reflex { get; set; }

    public bool PCL { get; set; }

    public bool NIS { get; set; }

    public bool DeepDVC { get; set; }

    public bool DirectSR { get; set; }

    public bool NvPerf { get; set; }

    /// <summary>
    /// Returns the selected feature IDs, including the Reflex and PCL dependencies.
    /// </summary>
    public readonly uint[] GetFeatures()
    {
        List<uint> features = [];

        if (DLSS)
        {
            features.Add(SL.FeatureDLSS);
        }

        if (DLSSD)
        {
            features.Add(SL.FeatureDLSSRR);
        }

        if (DLSSG)
        {
            features.Add(SL.FeatureDLSSG);
        }

        if (Reflex || DLSSG)
        {
            features.Add(SL.FeatureReflex);
        }

        if (PCL || Reflex || DLSSG)
        {
            features.Add(SL.FeaturePCL);
        }

        if (NIS)
        {
            features.Add(SL.FeatureNIS);
        }

        if (DeepDVC)
        {
            features.Add(SL.FeatureDeepDVC);
        }

        if (DirectSR)
        {
            features.Add(SL.FeatureDirectSR);
        }

        if (NvPerf)
        {
            features.Add(SL.FeatureNvPerf);
        }

        return [.. features];
    }
}
