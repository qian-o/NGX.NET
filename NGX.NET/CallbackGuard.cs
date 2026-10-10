using System.Diagnostics;

namespace NGX.NET;

internal static class CallbackGuard
{
    internal static NGXAppLogCallback? Wrap(NGXAppLogCallback? callback)
    {
        if (callback is null)
        {
            return null;
        }

        return (message, level, component) =>
        {
            try
            {
                callback(message, level, component);
            }
            catch (Exception exception)
            {
                Report(exception);
            }
        };
    }

    internal static NGXPfnProgressCallback? Wrap(NGXPfnProgressCallback? callback)
    {
        if (callback is null)
        {
            return null;
        }

        return (float progress, ref bool cancel) =>
        {
            try
            {
                callback(progress, ref cancel);
            }
            catch (Exception exception)
            {
                cancel = true;
                Report(exception);
            }
        };
    }

    internal static NGXPfnProgressCallbackC? Wrap(NGXPfnProgressCallbackC? callback)
    {
        if (callback is null)
        {
            return null;
        }

        return (float progress, ref bool cancel) =>
        {
            try
            {
                callback(progress, ref cancel);
            }
            catch (Exception exception)
            {
                cancel = true;
                Report(exception);
            }
        };
    }

    internal static void Report(Exception exception)
    {
        try
        {
            Trace.TraceError("NGX callback failed: {0}", exception);
        }
        catch
        {
            // A trace listener must never unwind through native code.
        }
    }
}
