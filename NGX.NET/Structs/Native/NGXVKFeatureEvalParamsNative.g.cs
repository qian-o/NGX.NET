namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXVKFeatureEvalParamsNative(in NGXVKFeatureEvalParams value, NativeScope scope)
{
    [FieldOffset(0)]
    public NGXResourceVKNative* PInColor = value.Color is NGXResourceVK color ? scope.Alloc(new NGXResourceVKNative(in color)) : null;

    [FieldOffset(8)]
    public NGXResourceVKNative* PInOutput = value.Output is NGXResourceVK output ? scope.Alloc(new NGXResourceVKNative(in output)) : null;

    [FieldOffset(16)]
    public float InSharpness = value.Sharpness;
}
