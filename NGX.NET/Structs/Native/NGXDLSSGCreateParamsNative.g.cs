#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXDLSSGCreateParamsNative : IDisposable
{
    [FieldOffset(0)]
    public uint Width;

    [FieldOffset(4)]
    public uint Height;

    [FieldOffset(8)]
    public uint NativeBackbufferFormat;

    [FieldOffset(12)]
    public uint RenderWidth;

    [FieldOffset(16)]
    public uint RenderHeight;

    [FieldOffset(20)]
    public Bool8 DynamicResolutionScaling;

    public NGXDLSSGCreateParamsNative(in NGXDLSSGCreateParams value)
    {
        try
        {
            Width = value.Width;
            Height = value.Height;
            NativeBackbufferFormat = value.NativeBackbufferFormat;
            RenderWidth = value.RenderWidth;
            RenderHeight = value.RenderHeight;
            DynamicResolutionScaling = value.DynamicResolutionScaling;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        this = default;
    }
}
