namespace NGX.NET;

public class NGXException(NGXResult result, string? operation = null) : Exception($"{operation ?? "NGX"}: {result} (0x{(uint)result:X8})")
{
    public NGXResult Result { get; } = result;
}
