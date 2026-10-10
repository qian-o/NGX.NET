namespace NGX.NET;

public static unsafe partial class Ngx
{
    internal const string LibraryName = "ngx-bridge";

    public static string RuntimeDirectory => NativeLoader.RuntimeDirectory;

    public static partial class Parameter
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_Parameter_Reset")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void ResetNative(NGXParameter parameters);

        public static void Reset(NGXParameter parameters)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            ResetNative(parameters);
            NativeLifetime.Release(parameters);
        }
    }
}
