namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXDLSSGCreateParams
{
    public uint Width;

    public uint Height;

    public uint NativeBackbufferFormat;

    public uint RenderWidth;

    public uint RenderHeight;

    public Bool8 DynamicResolutionScaling;
}
