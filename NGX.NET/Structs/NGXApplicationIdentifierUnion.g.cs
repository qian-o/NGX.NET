namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct NGXApplicationIdentifierUnion
{
    [FieldOffset(0)]
    public NGXProjectIdDescription ProjectDesc;

    [FieldOffset(0)]
    public ulong ApplicationId;
}
