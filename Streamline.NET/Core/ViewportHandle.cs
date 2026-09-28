namespace Streamline.NET;

public unsafe partial struct ViewportHandle
{
    /// <summary>
    /// Reads the private native viewport value without changing its header.
    /// </summary>
    public static implicit operator uint(ViewportHandle handle)
    {
        return handle.value;
    }
}
