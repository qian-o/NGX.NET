using NGX.NET;

namespace Consumer;

internal static class Program
{
    private static int Main()
    {
        // The compiler checks public signatures with unsafe disabled without executing native entry points.
        NGXFeatureCommonInfo common = new()
        {
            PathListInfo = new()
            {
                Paths = ["/runtime/\u4E2D\u6587\U0001F680"]
            }
        };
        NGXDLSSCreateParams create = new()
        {
            Feature = new()
            {
                Width = 1280,
                Height = 720,
                TargetWidth = 1920,
                TargetHeight = 1080,
                PerfQualityValue = NGXPerfQualityValue.MaxQuality
            }
        };
        Action invoke = () =>
        {
            Ngx.D3D12.InitWithProjectID("project", NGXEngineType.Custom, "1.0", "/tmp", 1, common, NGXVersion.Api);
            Ngx.D3D12.AllocateParameters(out NGXParameter parameters);
            Ngx.Parameter.SetUI(parameters, Ngx.ParameterWidth, 1280);
            Ngx.Parameter.GetI(parameters, Ngx.ParameterSuperSamplingAvailable, out int available);
            Ngx.D3D12.CreateDLSSExt(1, 1, 1, out NGXHandle handle, parameters, create);
            Ngx.D3D12.ReleaseFeature(handle);
            Ngx.D3D12.DestroyParameters(parameters);
            Ngx.D3D12.Shutdown1(1);
        };
        GC.KeepAlive(invoke);
        Action invokeResults = () =>
        {
            NGXParameter parameters = Ngx.D3D12.AllocateParameters();
            int available = Ngx.Parameter.GetI(parameters, Ngx.ParameterSuperSamplingAvailable);
            NGXHandle handle = Ngx.D3D12.CreateDLSSExt(1, 1, 1, parameters, create);
            Extensions extensions = Ngx.Vulkan.RequiredExtensions();
            OptimalSettings settings = Ngx.DLSS.GetOptimalSettings(parameters, 1920, 1080, NGXPerfQualityValue.MaxQuality);
            settings = Ngx.DLSSD.GetOptimalSettings(parameters, 1920, 1080, NGXPerfQualityValue.MaxQuality);
            Stats1 stats = Ngx.DLSS.GetStats1(parameters);
            Stats2 extendedStats = Ngx.DLSSD.GetStats2(parameters);
            NGXFeatureDiscoveryInfo discovery = new();
            NGXFeatureRequirement requirement = Ngx.D3D12.GetFeatureRequirements(1, in discovery);
            NGXVkExtensionProperties[] properties = Ngx.Vulkan.GetFeatureInstanceExtensionRequirements(in discovery);
            NGXCUDADevice cudaDevice = new();
            NGXHandle cudaHandle = Ngx.CUDA.CreateFeature1(in cudaDevice, NGXFeature.SuperSampling, parameters);
            cudaHandle = Ngx.CUDA.CreateFeature1((NGXCUDADevice?)null, NGXFeature.SuperSampling, parameters);
            Ngx.D3D12.ReleaseFeature(handle);
            Ngx.D3D12.DestroyParameters(parameters);
        };
        GC.KeepAlive(invokeResults);
        Console.WriteLine("PASS safe consumer: production public API compiles with unsafe disabled.");

        return 0;
    }
}
