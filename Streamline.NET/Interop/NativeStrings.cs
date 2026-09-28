namespace Streamline.NET;

internal static unsafe class NativeStrings
{
    internal static bool Equals(sbyte* value, ReadOnlySpan<byte> expected)
    {
        for (int index = 0; index < expected.Length; index++)
        {
            if ((byte)value[index] != expected[index])
            {
                return false;
            }
        }

        return value[expected.Length] == 0;
    }
}
