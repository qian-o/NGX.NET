namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXDLSSGCreateParamsNative(in NGXDLSSGCreateParams value)
{
    [FieldOffset(0)]
    public uint Width = value.Width;

    [FieldOffset(4)]
    public uint Height = value.Height;

    [FieldOffset(8)]
    public uint NativeBackbufferFormat = value.NativeBackbufferFormat;

    [FieldOffset(12)]
    public uint RenderWidth = value.RenderWidth;

    [FieldOffset(16)]
    public uint RenderHeight = value.RenderHeight;

    [FieldOffset(20)]
    public Bool8 DynamicResolutionScaling = value.DynamicResolutionScaling;
}
