using System.Diagnostics;

namespace NGX.NET;

internal static partial class NgxCallbacks
{
    private static readonly Lock gate = new();
    private static readonly Dictionary<nint, Delegate> roots = [];

    internal static int Count
    {
        get
        {
            using Lock.Scope _ = gate.EnterScope();

            return roots.Count;
        }
    }

    internal static void Release(nint pointer)
    {
        if (pointer is 0)
        {
            return;
        }

        using Lock.Scope _ = gate.EnterScope();

        roots.Remove(pointer);
    }

    private static nint Register<T>(T callback)
        where T : Delegate
    {
        nint pointer = Marshal.GetFunctionPointerForDelegate(callback);
        {
            using Lock.Scope _ = gate.EnterScope();

            roots.Add(pointer, callback);
        }

        return pointer;
    }

    private static void Report(Exception exception)
    {
        // A user callback or TraceListener must never unwind through native code.
        try
        {
            Trace.TraceError("NGX callback failed: {0}", exception);
        }
        catch
        {
        }
    }
}
