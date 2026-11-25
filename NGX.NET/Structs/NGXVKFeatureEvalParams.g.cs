namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXVKFeatureEvalParams
{
    public NGXResourceVK* Color;

    public NGXResourceVK* Output;

    public float Sharpness;
}
