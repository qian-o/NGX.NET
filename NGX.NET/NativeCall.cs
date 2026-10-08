using System.Runtime.InteropServices;

namespace NGX.NET;

// Internal ownership for SDK inputs retained after a call. No public scope or
// initialization object is required. All data constructors still live on Native.
internal sealed unsafe class NativeCall : IDisposable
{
    private NativeOwner? values;
    private List<nint>? strings;

    // The DLSSG helper leaves its matrix pointers unchanged when optional
    // settings are omitted. Their previous native storage must remain alive.
    internal bool HasFrameGenerationOptions { get; set; }

    internal void* String(string? value, NGXEncoding encoding)
    {
        void* pointer = NGXMarshal.TextToPtr(value, encoding);
        try { (strings ??= []).Add((nint)pointer); }
        catch { NGXMarshal.Free(pointer); throw; }
        return pointer;
    }

    internal T* Take<T>(ref T value) where T : unmanaged, IDisposable
    {
        NativeOwner<T> owner = new(ref value);
        owner.Next = values;
        values = owner;
        return owner.Pointer;
    }

    public void Dispose()
    {
        while (values is not null)
        {
            NativeOwner value = values;
            values = value.Next;
            value.Dispose();
        }
        if (strings is not null)
        {
            foreach (nint value in strings) NGXMarshal.Free((void*)value);
            strings.Clear();
        }
    }
}

internal abstract class NativeOwner : IDisposable
{
    internal NativeOwner? Next;

    public abstract void Dispose();
}

internal sealed unsafe class NativeOwner<T> : NativeOwner where T : unmanaged, IDisposable
{
    internal T* Pointer { get; private set; }

    internal NativeOwner(ref T value)
    {
        Pointer = NGXMarshal.AllocValue(value);
        value = default;
    }

    public override void Dispose()
    {
        NGXMarshal.FreeNative(Pointer);
        Pointer = null;
    }
}
