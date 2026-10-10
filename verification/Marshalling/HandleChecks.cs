namespace Marshalling;

internal static class HandleChecks
{
    internal static void Run()
    {
        NGXHandle handle = new(42);
        NGXHandle equalHandle = new(42);
        NGXParameter parameters = new(42);
        NGXParameter equalParameters = new(42);

        Assert(Marshal.SizeOf<NGXHandle>() == nint.Size && Marshal.SizeOf<NGXParameter>() == nint.Size, "Handle layout");
        Assert(typeof(NGXHandle).GetField(nameof(NGXHandle.Value))!.IsInitOnly && typeof(NGXParameter).GetField(nameof(NGXParameter.Value))!.IsInitOnly, "Readonly handle fields");
        Assert(handle == equalHandle && handle != default && handle.Equals((object)equalHandle), "Handle equality");
        Assert(parameters == equalParameters && parameters != default && parameters.Equals((object)equalParameters), "Parameter equality");
        Assert(handle.GetHashCode() == equalHandle.GetHashCode() && parameters.GetHashCode() == equalParameters.GetHashCode(), "Handle hash codes");
        Assert(handle.Value is 42 && parameters.Value is 42 && default(NGXHandle).IsNull && default(NGXParameter).IsNull, "Handle values");
        Assert(typeof(NGXHandle).GetMethod("Deconstruct") is null && typeof(NGXParameter).GetMethod("Deconstruct") is null, "Handle deconstruction removed");
        Assert(handle.ToString() is "NGXHandle { Value = 42, IsNull = False }", "Handle display");
        Assert(Enum.IsDefined(NGXDLSSGEvalFlags.None) && Enum.IsDefined(NGXFeatureSupportResult.None), "Empty flag values");

        Console.WriteLine("PASS readonly handles preserve layout, equality and hashing without deconstruction");
    }
}
