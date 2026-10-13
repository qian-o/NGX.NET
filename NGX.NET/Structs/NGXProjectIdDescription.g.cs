namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXProjectIdDescription
{
    public byte* ProjectId;

    public NGXEngineType EngineType;

    public byte* EngineVersion;
}
