namespace Marshalling;

internal static unsafe class GBufferChecks
{
    internal static void Run()
    {
        NGXVKGBuffer value = new()
        {
            Attributes = [Resource(0), null, Resource(2)]
        };
        NGXVKGBufferNative native = new(in value);
        Assert(native.PInAttrib[0].Value is not null && native.PInAttrib[1].Value is null && native.PInAttrib[16].Value is null, "Sparse array");
        NGXVKGBuffer restored = new(in native);
        Assert(restored.Attributes!.Length is 17 && restored.Attributes[2]!.Value.Resource.ImageViewInfo!.Value.Image is 0x1202, "Array conversion");
        native.Dispose();
        NGXCUDAGBuffer cuda = new()
        {
            Attributes = [42, null, 0]
        };
        NGXCUDAGBufferNative cudaNative = new(in cuda);
        Assert(*cudaNative.PInAttrib[0].Value is 42 && cudaNative.PInAttrib[1].Value is null && *cudaNative.PInAttrib[2].Value is 0, "CUDA scalar pointer presence");
        cudaNative.Dispose();

        Console.WriteLine("PASS GBuffer arrays preserve null slots and fixed native capacity");
    }
}
