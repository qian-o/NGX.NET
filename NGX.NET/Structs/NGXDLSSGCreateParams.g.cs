#nullable enable

namespace NGX.NET;

public struct NGXDLSSGCreateParams
{
    public uint Width;

    public uint Height;

    public uint NativeBackbufferFormat;

    public uint RenderWidth;

    public uint RenderHeight;

    public bool DynamicResolutionScaling;

    internal unsafe NGXDLSSGCreateParams(in NGXDLSSGCreateParamsNative native)
    {
        Width = native.Width;
        Height = native.Height;
        NativeBackbufferFormat = native.NativeBackbufferFormat;
        RenderWidth = native.RenderWidth;
        RenderHeight = native.RenderHeight;
        DynamicResolutionScaling = native.DynamicResolutionScaling;
    }
}
