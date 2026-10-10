namespace Marshalling;

internal static unsafe class CallbackChecks
{
    internal static void Run()
    {
        int calls = 0;
        int disposals = 0;
        TrackingScope scope = new(() => disposals++);
        (nint pointer, WeakReference<NGXAppLogCallback> callbackReference) = CreateLogging(scope, message =>
        {
            Assert(message is "\u65E5\u5FD7\U0001F680", "Callback UTF-8");
            Interlocked.Increment(ref calls);
        });
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        Assert(callbackReference.TryGetTarget(out _), "Scope retains the original logging callback through its guard");
        Parallel.For(0, 64, _ => Log(pointer));
        Assert(calls is 64, "Concurrent callback roots");
        scope.Dispose();
        scope.Dispose();
        Assert(scope.IsDisposed && disposals is 1, "Callback scope cleanup");
        NGXPfnProgressCallback progress = static (float _, ref bool cancel) =>
        {
            cancel = false;

            throw new InvalidOperationException("Expected callback exception.");
        };
        NGXPfnProgressCallback guarded = CallbackGuard.Wrap(progress)!;
        pointer = Marshal.GetFunctionPointerForDelegate(guarded);
        byte cancelled = 0;
        ((delegate* unmanaged[Cdecl]<float, byte*, void>)pointer)(0.5f, &cancelled);
        GC.KeepAlive(guarded);
        Assert(cancelled is 1, "One-byte ref bool and exception barrier");
        NGXPfnProgressCallbackC progressC = static (float _, ref bool cancel) =>
        {
            cancel = false;

            throw new InvalidOperationException("Expected callback exception.");
        };
        NGXPfnProgressCallbackC guardedC = CallbackGuard.Wrap(progressC)!;
        pointer = Marshal.GetFunctionPointerForDelegate(guardedC);
        cancelled = 0;
        ((delegate* unmanaged[Cdecl]<float, byte*, void>)pointer)(0.5f, &cancelled);
        GC.KeepAlive(guardedC);
        Assert(cancelled is 1, "C progress callback exception requests cancellation");
        Assert(CallbackGuard.Wrap((NGXAppLogCallback?)null) is null && CallbackGuard.Wrap((NGXPfnProgressCallback?)null) is null && CallbackGuard.Wrap((NGXPfnProgressCallbackC?)null) is null, "Omitted callbacks remain null");
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

        Console.WriteLine("PASS native callback ABI, scope roots, UTF-8 and exception cancellation guards");
    }

    private static (nint Pointer, WeakReference<NGXAppLogCallback> Callback) CreateLogging(NativeScope scope, Action<string?> log)
    {
        NGXAppLogCallback callback = (message, _, _) => log(message);
        NGXLoggingInfo value = new() { LoggingCallback = callback };
        NGXLoggingInfoNative native = new(in value, scope);

        return (native.LoggingCallback, new(callback));
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
