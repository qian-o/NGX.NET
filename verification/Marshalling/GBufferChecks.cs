namespace Marshalling;

internal static unsafe class GBufferChecks
{
    internal static void Run()
    {
        using NativeScope scope = new();
        NGXVKGBuffer value = new()
        {
            Attributes = [Resource(0), null, Resource(2)]
        };
        NGXVKGBufferNative native = new(in value, scope);
        Assert(native.PInAttrib[0] is not 0 && native.PInAttrib[1] is 0 && native.PInAttrib[16] is 0, "Sparse array");
        NGXResourceVK restored = new(in *(NGXResourceVKNative*)native.PInAttrib[2]);
        Assert(restored.Resource.ImageViewInfo!.Value.Image is 0x1202, "Array resource conversion");
        NGXCUDAGBuffer cuda = new()
        {
            Attributes = [42, null, 0]
        };
        NGXCUDAGBufferNative cudaNative = new(in cuda, scope);
        Assert(*(ulong*)cudaNative.PInAttrib[0] is 42 && cudaNative.PInAttrib[1] is 0 && *(ulong*)cudaNative.PInAttrib[2] is 0, "CUDA scalar pointer presence");
        scope.Dispose();
        Assert(restored.Resource.ImageViewInfo.Value.Image is 0x1202, "Managed resource independence after array scope cleanup");

        Console.WriteLine("PASS GBuffer arrays preserve null slots, explicit zeros and fixed native capacity");
    }
}
