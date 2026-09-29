namespace NGX.NET;

/// <summary>
/// Render dimensions and legacy sharpness returned by the official DLSS helpers.
/// </summary>
public readonly record struct NGXOptimalSettings(uint RenderWidth, uint RenderHeight, uint MaxWidth, uint MaxHeight, uint MinWidth, uint MinHeight, float Sharpness);

public static unsafe partial class NGX
{
    public static partial class DLSS
    {
        /// <summary>
        /// Queries optimal render dimensions and throws if the SDK reports failure.
        /// </summary>
        public static NGXOptimalSettings GetOptimalSettings(global::NGX.NET.NGXParameter* parameters, uint width, uint height, NGXPerfQualityValue quality)
        {
            uint renderWidth = 0, renderHeight = 0, maxWidth = 0, maxHeight = 0, minWidth = 0, minHeight = 0;
            float sharpness = 0;
            ThrowIfFailed(GetOptimalSettings(parameters, width, height, quality, &renderWidth, &renderHeight, &maxWidth, &maxHeight, &minWidth, &minHeight, &sharpness));

            return new(renderWidth, renderHeight, maxWidth, maxHeight, minWidth, minHeight, sharpness);
        }
    }

    public static partial class DLSSD
    {
        /// <summary>
        /// Queries optimal ray reconstruction dimensions and throws on failure.
        /// </summary>
        public static NGXOptimalSettings GetOptimalSettings(global::NGX.NET.NGXParameter* parameters, uint width, uint height, NGXPerfQualityValue quality)
        {
            uint renderWidth = 0, renderHeight = 0, maxWidth = 0, maxHeight = 0, minWidth = 0, minHeight = 0;
            float sharpness = 0;
            ThrowIfFailed(GetOptimalSettings(parameters, width, height, quality, &renderWidth, &renderHeight, &maxWidth, &maxHeight, &minWidth, &minHeight, &sharpness));

            return new(renderWidth, renderHeight, maxWidth, maxHeight, minWidth, minHeight, sharpness);
        }
    }
}
