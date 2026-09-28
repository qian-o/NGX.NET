namespace Streamline.NET;

public unsafe partial struct Resource
{
    /// <summary>
    /// Returns the borrowed ID3D12Resource address. No COM reference is acquired.
    /// </summary>
    public readonly nint AsD3D12Resource()
    {
        return (nint)Native;
    }

    /// <summary>
    /// Returns the borrowed ID3D11Resource address. No COM reference is acquired.
    /// </summary>
    public readonly nint AsD3D11Resource()
    {
        return (nint)Native;
    }

    /// <summary>
    /// Returns the borrowed ID3D11Buffer address. No COM reference is acquired.
    /// </summary>
    public readonly nint AsD3D11Buffer()
    {
        return (nint)Native;
    }

    /// <summary>
    /// Returns the borrowed ID3D11Texture2D address. No COM reference is acquired.
    /// </summary>
    public readonly nint AsD3D11Texture2D()
    {
        return (nint)Native;
    }
}
