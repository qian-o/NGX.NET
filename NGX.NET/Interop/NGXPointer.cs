using System.Runtime.InteropServices;

namespace NGX.NET;

/// <summary>
/// Blittable pointer element for native inline arrays. This value owns no memory.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXPointer<T> where T : unmanaged
{
    /// <summary>
    /// Native pointer value.
    /// </summary>
    public T* Value;

    /// <summary>
    /// Wraps a borrowed pointer.
    /// </summary>
    public static implicit operator NGXPointer<T>(T* value) => new() { Value = value };

    /// <summary>
    /// Retrieves the borrowed pointer.
    /// </summary>
    public static implicit operator T*(NGXPointer<T> value) => value.Value;
}
