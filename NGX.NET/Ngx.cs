namespace NGX.NET;

public static unsafe partial class Ngx
{
    internal const string LibraryName = "ngx-bridge";

    public static string RuntimeDirectory => NativeLoader.RuntimeDirectory;

    public static partial class Parameter
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_Parameter_Reset")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Reset(NGXParameter parameters);
    }
}
