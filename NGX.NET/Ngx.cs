using System.Text;

namespace NGX.NET;

public static unsafe partial class Ngx
{
    internal const string LibraryName = "ngx-bridge";

    public static string RuntimeDirectory => NativeLoader.RuntimeDirectory;

    public static string GetResultString(NGXResult ngxResult)
    {
        void* value = GetResultAsString(ngxResult);
        if (value is null)
        {
            return string.Empty;
        }

        if (OperatingSystem.IsWindows())
        {
            return Marshal.PtrToStringUni((nint)value) ?? string.Empty;
        }

        uint* cursor = (uint*)value;
        while (*cursor is not 0)
        {
            cursor++;
        }

        return Encoding.UTF32.GetString((byte*)value, checked((int)((cursor - (uint*)value) * sizeof(uint))));
    }

    public static partial class Parameter
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_Parameter_Reset")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Reset(NGXParameter parameters);
    }
}
