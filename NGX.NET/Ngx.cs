namespace NGX.NET;

public static unsafe partial class Ngx
{
    internal const string LibraryName = "ngx-bridge";

    public static string RuntimeDirectory => NativeLoader.RuntimeDirectory;

    public static partial class Parameter
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_Parameter_Reset")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void ResetNative(nint parameters);

        public static void Reset(NGXParameter parameters)
        {
            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX parameter handle is required.", nameof(parameters));
            }

            ResetNative(parameters.Value);
            NgxLifetime.ReleaseParameters(parameters.Value);
        }
    }
}
