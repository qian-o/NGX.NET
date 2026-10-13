namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXCUDAFeatureEvalParams
{
    public ulong* Color;

    public ulong* Output;

    public float Sharpness;
}
