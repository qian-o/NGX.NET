#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXProjectIdDescriptionNative
{
    [FieldOffset(0)]
    public byte* ProjectId;

    [FieldOffset(8)]
    public NGXEngineType EngineType;

    [FieldOffset(16)]
    public byte* EngineVersion;

    public NGXProjectIdDescriptionNative(in NGXProjectIdDescription value, NativeScope scope)
    {
        ProjectId = scope.AllocUtf8(value.ProjectId);
        EngineType = value.EngineType;
        EngineVersion = scope.AllocUtf8(value.EngineVersion);
    }
}
