namespace NGX.NET;

internal abstract class NativeOwner : IDisposable
{
    internal NativeOwner? Next;

    public abstract void Dispose();
}
