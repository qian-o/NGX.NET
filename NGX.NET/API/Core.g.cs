namespace NGX.NET;

public static unsafe partial class Ngx
{
    [LibraryImport(LibraryName, EntryPoint = "GetNGXResultAsString")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void* GetResultAsString(NGXResult ngxResult);

    [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NVSDK_NGX_Create_Buffer_Resource_VK")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial NGXResourceVK CreateBufferResourceVK(nint buffer, uint sizeInBytes, byte readWrite);

    [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NVSDK_NGX_Create_ImageView_Resource_VK")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial NGXResourceVK CreateImageViewResourceVK(nint imageView, nint image, NGXVkImageSubresourceRange subresourceRange, NGXVkFormat format, uint width, uint height, byte readWrite);

    [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_UpdateFeature")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial NGXResult UpdateFeature(NGXApplicationIdentifier* applicationId, NGXFeature featureID);

    static Ngx()
    {
        NativeLoader.Register();
    }

    public static NGXResourceVK CreateBufferResourceVK(nint buffer, uint sizeInBytes, bool readWrite)
    {
        return CreateBufferResourceVK(buffer, sizeInBytes, readWrite ? (byte)1 : (byte)0);
    }

    public static NGXResourceVK CreateImageViewResourceVK(nint imageView, nint image, NGXVkImageSubresourceRange subresourceRange, NGXVkFormat format, uint width, uint height, bool readWrite)
    {
        return CreateImageViewResourceVK(imageView, image, subresourceRange, format, width, height, readWrite ? (byte)1 : (byte)0);
    }

    public static NGXResult UpdateFeature(in NGXApplicationIdentifier applicationId, NGXFeature featureID)
    {
        fixed (NGXApplicationIdentifier* pApplicationId = &applicationId)
        {
            return UpdateFeature(pApplicationId, featureID);
        }
    }
}
