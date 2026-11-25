namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXResourceVK
{
    public NGXResourceVKUnion Resource;

    public NGXResourceVKType Type;

    public Bool8 ReadWrite;
}
