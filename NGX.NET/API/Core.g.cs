#nullable enable

namespace NGX.NET;

public static unsafe partial class Ngx
{
    [LibraryImport(LibraryName, EntryPoint = "GetNGXResultAsString")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void* GetResultAsStringNative(NGXResult inNGXResult);

    [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NVSDK_NGX_Create_Buffer_Resource_VK")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial NGXResourceVKNative CreateBufferResourceVKNative(nint buffer, uint sizeInBytes, Bool8 readWrite);

    [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NVSDK_NGX_Create_ImageView_Resource_VK")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial NGXResourceVKNative CreateImageViewResourceVKNative(nint imageView, nint image, NGXVkImageSubresourceRangeNative subresourceRange, NGXVkFormat format, uint width, uint height, Bool8 readWrite);

    [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_UpdateFeature")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial NGXResult UpdateFeatureNative(NGXApplicationIdentifierNative* applicationId, NGXFeature featureID);

    static Ngx()
    {
        NativeLoader.Register();
    }

    public static string? GetResultAsString(NGXResult ngxResult)
    {
        void* result = GetResultAsStringNative(ngxResult);

        return NGXMarshal.PtrToString(result, NGXEncoding.NativeWide);
    }

    public static NGXResourceVK CreateBufferResourceVK(nint buffer, uint sizeInBytes, bool readWrite)
    {
        NGXResourceVKNative result = CreateBufferResourceVKNative(buffer, sizeInBytes, readWrite);

        return new(in result);
    }

    public static NGXResourceVK CreateImageViewResourceVK(nint imageView, nint image, in NGXVkImageSubresourceRange subresourceRange, NGXVkFormat format, uint width, uint height, bool readWrite)
    {
        NGXVkImageSubresourceRangeNative subresourceRangeNative = default;

        try
        {
            subresourceRangeNative = new(in subresourceRange);
            NGXResourceVKNative result = CreateImageViewResourceVKNative(imageView, image, subresourceRangeNative, format, width, height, readWrite);

            return new(in result);
        }
        finally
        {
            subresourceRangeNative.Dispose();
        }
    }

    public static NGXResult UpdateFeature(in NGXApplicationIdentifier applicationId, NGXFeature featureID)
    {
        NGXApplicationIdentifierNative applicationIdNative = default;

        try
        {
            applicationIdNative = new(in applicationId);
            NGXResult result = UpdateFeatureNative(&applicationIdNative, featureID);

            return result;
        }
        finally
        {
            applicationIdNative.Dispose();
        }
    }
}
