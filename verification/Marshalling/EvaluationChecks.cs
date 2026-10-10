namespace Marshalling;

internal static unsafe class EvaluationChecks
{
    internal static void Run()
    {
        using NativeScope scope = new();
        NGXVKDLSSDEvalParams value = Frame();
        NGXVKDLSSDEvalParamsNative native = new(in value, scope);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        NGXResourceVKNative*[] ports = [native.PInDiffuseAlbedo, native.PInSpecularAlbedo, native.PInNormals, native.PInRoughness, native.PInColor, native.PInOutput, native.PInDepth, native.PInMotionVectors, native.PInExposureTexture, native.PInBiasCurrentColorMask, native.PInColorBeforeTransparency, native.PInScreenSpaceSubsurfaceScatteringGuide, native.PInDepthOfFieldGuide, native.PInSpecularHitDistance, native.PInMotionVectorsReflections, native.PInTransparencyLayer, native.PInTransparencyLayerOpacity];
        for (int i = 0; i < ports.Length; i++)
        {
            Assert(ports[i]->Resource.ImageViewInfo.Image == 0x1200 + i, "Resource port mapping " + i);
        }

        Assert((bool)native.PInOutput->ReadWrite, "Native bool width/value");
        Assert(native.PInColor->Resource.ImageViewInfo.SubresourceRange.BaseMipLevel is 3 && native.PInColor->Resource.ImageViewInfo.SubresourceRange.BaseArrayLayer is 2, "Selected view");
        float* matrix = (float*)native.PInWorldToViewMatrix;
        Assert(matrix[12] == 3 && matrix[13] == 5 && matrix[14] == 7 && matrix[15] == 1, "Native float matrix order");
        NGXResourceVK read = new(in *native.PInColor);
        Matrix4x4 worldToView = *native.PInWorldToViewMatrix;
        scope.Dispose();
        Assert(read.Resource.ImageViewInfo!.Value.Image is 0x1204 && worldToView == value.WorldToViewMatrix, "Owned managed resource and matrix copy");
        value.ExposureTexture = null;
        value.WorldToViewMatrix = default(Matrix4x4);
        value.ViewToClipMatrix = null;
        using NativeScope optionalScope = new();
        native = new(in value, optionalScope);
        Assert(native.PInExposureTexture is null && native.PInWorldToViewMatrix is not null && native.PInViewToClipMatrix is null, "Optional versus explicit zero");

        Console.WriteLine("PASS RR resource and matrix pointers survive constructor return and preserve selected views");
    }
}
