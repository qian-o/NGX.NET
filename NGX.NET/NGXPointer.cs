namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NGXPointer<T>
    where T : unmanaged
{
    public T* Value;

    public static implicit operator NGXPointer<T>(T* value)
    {
        return new()
        {
            Value = value
        };
    }

    public static implicit operator T*(NGXPointer<T> value)
    {
        return value.Value;
    }
}
