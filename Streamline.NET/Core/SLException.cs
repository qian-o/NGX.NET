namespace Streamline.NET;

/// <summary>A non-success result returned by a Streamline SDK operation.</summary>
/// <remarks>
/// Thrown only by convenience overloads that return their output value directly.
/// The result-returning pointer and reference overloads preserve the SDK result.
/// </remarks>
#pragma warning disable RCS1194 // An SDK exception requires its original result and operation; message-only constructors would discard that contract.
public sealed class SLException : Exception
{
    /// <summary>Creates an exception retaining the original SDK result and native function name.</summary>
    public SLException(SLResult result, string nativeFunction)
        : base($"Streamline function {nativeFunction} returned {result} ({(int)result}).")
    {
        Result = result;
        NativeFunction = nativeFunction;
    }

    /// <summary>The original SDK result, including any non-success warning result.</summary>
    public SLResult Result { get; }

    /// <summary>The original SDK function name, such as slDLSSGetOptimalSettings.</summary>
    public string NativeFunction { get; }
}
#pragma warning restore RCS1194
