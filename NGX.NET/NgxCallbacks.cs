using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NGX.NET;

internal static partial class NgxCallbacks
{
    private static readonly Lock gate = new();
    private static readonly Dictionary<nint, Delegate> roots = [];

    private static nint Register<T>(T callback) where T : Delegate
    {
        nint pointer = Marshal.GetFunctionPointerForDelegate(callback);
        lock (gate) roots.Add(pointer, callback);
        return pointer;
    }

    internal static void Release(nint pointer)
    {
        if (pointer == 0) return;
        lock (gate) roots.Remove(pointer);
    }

    private static void Report(Exception exception)
    {
        // A user callback or TraceListener must never unwind through native code.
        try { Trace.TraceError("NGX callback failed: {0}", exception); }
        catch { }
    }
}
