using System.Runtime.InteropServices;

namespace NGX.NET;

/// <summary>
/// Blittable pointer element for native inline arrays. This value owns no memory.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Pointer<T> where T : unmanaged
{
    /// <summary>
    /// Native pointer value.
    /// </summary>
    public T* Value;

    /// <summary>
    /// Wraps a borrowed pointer.
    /// </summary>
    public static implicit operator Pointer<T>(T* value) => new() { Value = value };

    /// <summary>
    /// Retrieves the borrowed pointer.
    /// </summary>
    public static implicit operator T*(Pointer<T> value) => value.Value;
}
