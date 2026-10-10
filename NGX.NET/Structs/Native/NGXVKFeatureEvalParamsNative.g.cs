#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXVKFeatureEvalParamsNative : IDisposable
{
    [FieldOffset(0)]
    public NGXResourceVKNative* PInColor;

    [FieldOffset(8)]
    public NGXResourceVKNative* PInOutput;

    [FieldOffset(16)]
    public float InSharpness;

    public NGXVKFeatureEvalParamsNative(in NGXVKFeatureEvalParams value)
    {
        this = default;

        try
        {
            if (value.Color is NGXResourceVK color)
            {
                PInColor = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in color));
            }

            if (value.Output is NGXResourceVK output)
            {
                PInOutput = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in output));
            }

            InSharpness = value.Sharpness;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        NGXMarshal.FreeNative(PInOutput);
        NGXMarshal.FreeNative(PInColor);
        this = default;
    }
}
