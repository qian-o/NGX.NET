namespace Streamline.NET;

public unsafe partial struct ReflexHelper
{
    /// <summary>
    /// Returns the stored native marker value.
    /// </summary>
    public static implicit operator uint(ReflexHelper value)
    {
        return value.marker;
    }
}
