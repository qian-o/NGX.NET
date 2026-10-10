#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXApplicationIdentifierUnionNative : IDisposable
{
    [FieldOffset(0)]
    public NGXProjectIdDescriptionNative ProjectDesc;

    [FieldOffset(0)]
    public ulong ApplicationId;

    public NGXApplicationIdentifierUnionNative(in NGXApplicationIdentifierUnion value)
    {
        this = default;

        try
        {
            if (value.ProjectDesc is NGXProjectIdDescription projectDesc)
            {
                ProjectDesc = new(in projectDesc);
            }
            else
            {
                ApplicationId = value.ApplicationId;
            }
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
