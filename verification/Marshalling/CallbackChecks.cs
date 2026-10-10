namespace Marshalling;

internal static unsafe class CallbackChecks
{
    internal static void Run()
    {
        int calls = 0;
        NGXLoggingInfo value = new()
        {
            LoggingCallback = (message, _, _) =>
            {
                Assert(message is "\u65E5\u5FD7\U0001F680", "Callback UTF-8");
                Interlocked.Increment(ref calls);
            }
        };
        NGXLoggingInfoNative native = new(in value);
        nint pointer = native.LoggingCallback;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        Parallel.For(0, 64, _ => Log(pointer));
        Assert(calls is 64, "Concurrent callback roots");
        native.Dispose();
        Assert(NgxCallbacks.Count is 0, "Callback cleanup");
        NGXPfnProgressCallback progress = static (float _, ref bool cancel) =>
        {
            cancel = true;

            throw new InvalidOperationException("Expected callback exception");
        };
        pointer = NgxCallbacks.Acquire(progress);
        byte cancelled = 0;
        ((delegate* unmanaged[Cdecl]<float, byte*, void>)pointer)(0.5f, &cancelled);
        Assert(cancelled is 1, "One-byte ref bool and exception barrier");
        NgxCallbacks.Release(pointer);
        bool seen = false;
        int callbackCalls = 0;
        NGXPfnDLSSGetStatsCallback callback = parameters =>
        {
            seen = parameters.Value is 123;
            callbackCalls++;

            return NGXResult.Success;
        };
        pointer = Marshal.GetFunctionPointerForDelegate(callback);
        NGXResult callbackResult = ((delegate* unmanaged[Cdecl]<nint, NGXResult>)pointer)(123);
        GC.KeepAlive(callback);
        Assert(seen && callbackCalls is 1 && callbackResult is NGXResult.Success, "Opaque callback handle ABI");
        Assert(NgxCallbacks.Count is 0, "All callback roots released");

        Console.WriteLine("PASS native callback ABI, UTF-8 strings, exceptions and one-byte cancellation");
    }

    private static void Log(nint pointer)
    {
        ReadOnlySpan<byte> bytes = "\u65E5\u5FD7\U0001F680\0"u8;
        fixed (byte* text = bytes)
        {
            ((delegate* unmanaged[Cdecl]<byte*, NGXLoggingLevel, NGXFeature, void>)pointer)(text, NGXLoggingLevel.On, NGXFeature.SuperSampling);
        }
    }
}
