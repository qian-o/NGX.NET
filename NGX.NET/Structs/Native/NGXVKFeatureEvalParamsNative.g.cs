#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXVKFeatureEvalParamsNative
{
    [FieldOffset(0)]
    public NGXResourceVKNative* PInColor;

    [FieldOffset(8)]
    public NGXResourceVKNative* PInOutput;

    [FieldOffset(16)]
    public float InSharpness;

    public NGXVKFeatureEvalParamsNative(in NGXVKFeatureEvalParams value, NativeScope scope)
    {
        PInColor = value.Color is NGXResourceVK color ? scope.Alloc(new NGXResourceVKNative(in color)) : null;
        PInOutput = value.Output is NGXResourceVK output ? scope.Alloc(new NGXResourceVKNative(in output)) : null;
        InSharpness = value.Sharpness;
    }
}
