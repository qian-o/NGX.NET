namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXPathListInfo
{
    public void** Path;

    public uint Length;
}
