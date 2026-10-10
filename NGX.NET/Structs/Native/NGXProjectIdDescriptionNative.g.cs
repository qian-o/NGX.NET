namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXProjectIdDescriptionNative(in NGXProjectIdDescription value, NativeScope scope)
{
    [FieldOffset(0)]
    public byte* ProjectId = scope.AllocUtf8(value.ProjectId);

    [FieldOffset(8)]
    public NGXEngineType EngineType = value.EngineType;

    [FieldOffset(16)]
    public byte* EngineVersion = scope.AllocUtf8(value.EngineVersion);
}
