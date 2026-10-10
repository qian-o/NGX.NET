#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXProjectIdDescriptionNative : IDisposable
{
    [FieldOffset(0)]
    public sbyte* ProjectId;

    [FieldOffset(8)]
    public NGXEngineType EngineType;

    [FieldOffset(16)]
    public sbyte* EngineVersion;

    public NGXProjectIdDescriptionNative(in NGXProjectIdDescription value)
    {
        try
        {
            ProjectId = (sbyte*)NGXMarshal.TextToPtr(value.ProjectId, NGXEncoding.Utf8);
            EngineType = value.EngineType;
            EngineVersion = (sbyte*)NGXMarshal.TextToPtr(value.EngineVersion, NGXEncoding.Utf8);
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        NGXMarshal.Free(EngineVersion);
        NGXMarshal.Free(ProjectId);
        this = default;
    }
}
