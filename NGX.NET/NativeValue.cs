namespace NGX.NET;

internal unsafe class NativeValue<T> : NativeOwner where T : unmanaged, IDisposable
{
    internal NativeValue(ref T value)
    {
        Pointer = NGXMarshal.AllocValue(value);
        value = default;
    }

    internal T* Pointer { get; private set; }

    public override void Dispose()
    {
        NGXMarshal.FreeNative(Pointer);
        Pointer = null;
    }
}
