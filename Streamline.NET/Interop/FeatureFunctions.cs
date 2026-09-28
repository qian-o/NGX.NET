namespace Streamline.NET;

internal static unsafe class FeatureFunctions
{
    private static readonly object sync = new();

    private static readonly Dictionary<(uint Feature, string Name), nint> addresses = [];

    private static ulong generation;

    internal static SLResult Get(uint feature, string name, ReadOnlySpan<byte> encodedName, out nint address)
    {
        (uint, string) key = (feature, name);
        ulong queriedGeneration;

        lock (sync)
        {
            if (addresses.TryGetValue(key, out address))
            {
                return SLResult.Ok;
            }

            queriedGeneration = generation;
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
            lock (sync)
            {
                if (generation == queriedGeneration)
                {
                    addresses[key] = function;
                }
            }
        }

        return result;
    }

    internal static void Invalidate()
    {
        lock (sync)
        {
            generation++;
            addresses.Clear();
        }
    }
}
