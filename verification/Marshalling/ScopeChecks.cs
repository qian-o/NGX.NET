namespace Marshalling;

internal static unsafe class ScopeChecks
{
    internal static void Run()
    {
        int disposals = 0;
        TrackingScope scope = new(() => disposals++);
        int value = 123;
        int* copied = scope.Alloc(in value);
        int* zeroed = scope.Alloc<int>(3);
        Assert(*copied is 123 && zeroed[0] is 0 && zeroed[1] is 0 && zeroed[2] is 0, "Native value copy and zeroed arrays");
        Assert(scope.Alloc<int>(0) is null && scope.AllocUtf8(null) is null && scope.AllocWide(null) is null, "Null and empty native allocations");
        byte* utf8 = scope.AllocUtf8("before\0after");
        Assert(Marshal.PtrToStringUTF8((nint)utf8) is "before", "UTF-8 embedded NUL follows C string semantics");
        byte* replacement = scope.AllocUtf8("\uD800");
        Assert(Marshal.PtrToStringUTF8((nint)replacement) is "\uFFFD", "UTF-8 replacement fallback");
        Assert(NativeTextHelper.ReadWide(scope.AllocWide("before\0after")) is "before", "Native-wide embedded NUL follows C string semantics");
        Assert(NativeTextHelper.ReadWide(scope.AllocWide("\uD800")) is "\uFFFD", "Native-wide replacement fallback");
        scope.Dispose();
        scope.Dispose();
        Assert(scope.IsDisposed && disposals is 1, "Idempotent scope cleanup");
        int finalizations = 0;
        WeakReference<TrackingScope> abandoned = AbandonFailedConversion(() => Interlocked.Increment(ref finalizations));
        const int MaxCollectionAttempts = 3;

        for (int i = 0; i < MaxCollectionAttempts && Volatile.Read(ref finalizations) is 0; i++)
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
        }

        Assert(Volatile.Read(ref finalizations) is 1 && !abandoned.TryGetTarget(out _), "Abandoned conversion storage is released by the finalizer once");

        Console.WriteLine("PASS native scopes copy values, zero arrays, accept native text and release once explicitly or after abandoned conversion failure");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<TrackingScope> AbandonFailedConversion(Action destroyed)
    {
        TrackingScope scope = new(destroyed);
        NGXFeatureDiscoveryInfo value = Discovery();
        value.FeatureInfo = new()
        {
            PathListInfo = new()
            {
                Paths = ["allocated-before-failure", null!]
            }
        };

        try
        {
            _ = new NGXFeatureDiscoveryInfoNative(in value, scope);
        }
        catch (ArgumentNullException)
        {
            return new(scope);
        }

        scope.Dispose();

        throw new InvalidOperationException("Expected conversion failure after allocating native storage.");
    }

}
