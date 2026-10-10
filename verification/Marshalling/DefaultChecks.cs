namespace Marshalling;

internal static class DefaultChecks
{
    internal static void Run()
    {
        NGXDLSSGOptEvalParams value = new();
        NGXDLSSGOptEvalParamsNative native = new(in value);
        Assert(native.MultiFrameCount is 1 && native.MultiFrameIndex is 1 && native.MinRelativeLinearDepthObjectSeparation == 40, "SDK defaults");
        value = default;
        native = new(in value);
        Assert(native.MultiFrameCount is 0 && native.MinRelativeLinearDepthObjectSeparation == 0, "Explicit zeros");

        Console.WriteLine("PASS SDK defaults preserve the distinction between new and default");
    }
}
