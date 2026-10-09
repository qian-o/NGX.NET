namespace NGX.NET;

/// <summary>
/// An NGX operation failed according to NVSDK_NGX_FAILED.
/// </summary>
public class NGXException(NGXResult result, string? operation = null) : Exception($"{operation ?? "NGX"}: {result} (0x{(uint)result:X8})")
{
    /// <summary>
    /// Original result returned by the SDK.
    /// </summary>
    public NGXResult Result { get; } = result;
}
