namespace Streamline.NET;

internal sealed unsafe partial class FeatureFunctions
{
    private static FeatureFunctions current = new();

    internal static FeatureFunctions Current => Volatile.Read(ref current);

    private static SLResult Resolve(ref nint slot, uint feature, ReadOnlySpan<byte> encodedName, out nint address)
    {
        address = Volatile.Read(ref slot);

        if (address != 0)
        {
            return SLResult.Ok;
        }

        // All callers supply compiler-emitted, null-terminated UTF-8 literals.
        nint function = 0;
        SLResult result;

        fixed (byte* pointer = encodedName)
        {
            result = SLNative.GetFeatureFunction(feature, (sbyte*)pointer, (void**)&function);
        }

        address = function;

        if (result == SLResult.Ok)
        {
            nint cached = Interlocked.CompareExchange(ref slot, function, 0);

            if (cached != 0)
            {
                address = cached;
            }
        }

        return result;
    }

    internal static void Invalidate()
    {
        // In-flight resolutions may populate the old table, never the replacement.
        // Native calls must still obey the SDK's shutdown and feature-change sequencing.
        Interlocked.Exchange(ref current, new());
    }
}
