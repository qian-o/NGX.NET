#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXApplicationIdentifierUnionNative
{
    [FieldOffset(0)]
    public NGXProjectIdDescriptionNative ProjectDesc;

    [FieldOffset(0)]
    public ulong ApplicationId;

    public NGXApplicationIdentifierUnionNative(in NGXApplicationIdentifierUnion value, NativeScope scope)
    {
        this = default;
        if (value.ProjectDesc is NGXProjectIdDescription projectDesc)
        {
            ProjectDesc = new(in projectDesc, scope);
        }
        else
        {
            ApplicationId = value.ApplicationId;
        }
    }
}
