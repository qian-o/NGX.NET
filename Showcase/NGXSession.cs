using NGX.NET;
using Showcase.Models;

namespace Showcase;

internal sealed partial class NGXSession : IDisposable
{
    public Dictionary<NGXFeature, string> Unavailable { get; } = [];

    private const string ProjectId = "fc6ac847-10b0-48e1-842d-1bc819f8d2f4";
    private const string EngineVersion = "NGX.NET.Showcase.1.0";
    private readonly string dataPath;
    private readonly NGXFeatureCommonInfo common;
    private bool isVulkan;
    private nint device;
    private NGXParameter capabilities;
    private NGXParameter parameters;
    private NGXParameter frameParameters;
    private NGXHandle reconstruction;
    private NGXHandle generation;
    private RenderSettings settings = new();
    private int inputWidth, inputHeight, outputWidth, outputHeight;
    private bool initialized;

    public NGXSession()
    {
        dataPath = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(dataPath);
        common = new() { PathListInfo = new() { Paths = [Ngx.RuntimeDirectory] } };
    }

    public bool Available(NGXFeature feature) => !Unavailable.ContainsKey(feature);

    public void Initialize(nint nativeDevice, nint instance = 0, nint physical = 0, nint getInstanceProcAddr = 0, nint getDeviceProcAddr = 0)
    {
        isVulkan = instance != 0;
        device = nativeDevice;
        NGXResult result = isVulkan
            ? Ngx.Vulkan.InitWithProjectID(ProjectId, NGXEngineType.Custom, EngineVersion, dataPath, instance, physical, device, getInstanceProcAddr, getDeviceProcAddr, in common, NGXVersion.Api)
            : Ngx.D3D12.InitWithProjectID(ProjectId, NGXEngineType.Custom, EngineVersion, dataPath, device, in common, NGXVersion.Api);

        if (result is NGXResult.FailFeatureNotSupported or NGXResult.FailPlatformError or NGXResult.FailOutOfDate)
        {
            foreach (NGXFeature feature in new[] { NGXFeature.SuperSampling, NGXFeature.RayReconstruction, NGXFeature.FrameGeneration })
            {
                Unavailable[feature] = $"NGX initialization: {result}";
            }
            Console.WriteLine($"NGX features unavailable: {result}. Native rendering remains available.");
            return;
        }

        Ngx.ThrowIfFailed(result);
        initialized = true;
        Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.GetCapabilityParameters(out capabilities) : Ngx.D3D12.GetCapabilityParameters(out capabilities));
        Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.AllocateParameters(out parameters) : Ngx.D3D12.AllocateParameters(out parameters));
        Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.AllocateParameters(out frameParameters) : Ngx.D3D12.AllocateParameters(out frameParameters));

        Query(NGXFeature.SuperSampling, Ngx.ParameterSuperSamplingAvailable);
        Query(NGXFeature.RayReconstruction, Ngx.ParameterSuperSamplingDenoisingAvailable);
        Query(NGXFeature.FrameGeneration, Ngx.ParameterFrameGenerationAvailable);
    }

    public string[] VulkanExtensions(nint instance = 0, nint physical = 0)
    {
        HashSet<string> extensions = [];
        foreach (NGXFeature feature in new[] { NGXFeature.SuperSampling, NGXFeature.RayReconstruction, NGXFeature.FrameGeneration })
        {
            NGXFeatureDiscoveryInfo discovery = new()
            {
                SDKVersion = NGXVersion.Api,
                FeatureID = feature,
                Identifier = new()
                {
                    IdentifierType = NGXApplicationIdentifierType.ProjectId,
                    V = new()
                    {
                        ProjectDesc = new()
                        {
                            ProjectId = ProjectId,
                            EngineType = NGXEngineType.Custom,
                            EngineVersion = EngineVersion
                        }
                    }
                },
                ApplicationDataPath = dataPath,
                FeatureInfo = common
            };

            NGXVkExtensionProperties[] properties;
            NGXResult result = instance == 0
                ? Ngx.Vulkan.GetFeatureInstanceExtensionRequirements(in discovery, out properties)
                : Ngx.Vulkan.GetFeatureDeviceExtensionRequirements(instance, physical, in discovery, out properties);
            if (Ngx.Failed(result))
            {
                Unavailable[feature] = $"Extension requirements: {result}";
                continue;
            }
            foreach (NGXVkExtensionProperties property in properties) extensions.Add(property.ExtensionName!);
        }
        return [.. extensions];
    }

    private void Query(NGXFeature feature, string name)
    {
        NGXResult result = Ngx.Parameter.GetI(capabilities, name, out int available);
        if (Ngx.Failed(result) || available == 0)
        {
            Unavailable[feature] = Ngx.Failed(result) ? result.ToString() : "Not supported by this device/driver";
        }
        Console.WriteLine($"{feature}: {(Available(feature) ? "Available" : Unavailable[feature])}");
    }

    // The caller completes submitted GPU work before reconfiguration or disposal.
    public void ReleaseReconstruction() => Release(ref reconstruction);

    public void ReleaseFrameGeneration() => Release(ref generation);

    private void Release(ref NGXHandle handle)
    {
        if (!handle.IsNull)
        {
            Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.ReleaseFeature(handle) : Ngx.D3D12.ReleaseFeature(handle));
            handle = default;
        }
    }

    private void DestroyParameters(ref NGXParameter value)
    {
        if (!value.IsNull)
        {
            Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.DestroyParameters(value) : Ngx.D3D12.DestroyParameters(value));
            value = default;
        }
    }

    public void Dispose()
    {
        if (initialized)
        {
            ReleaseReconstruction();
            ReleaseFrameGeneration();
            DestroyParameters(ref frameParameters);
            DestroyParameters(ref parameters);
            DestroyParameters(ref capabilities);
            Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.Shutdown1(device) : Ngx.D3D12.Shutdown1(device));
            initialized = false;
        }
    }
}
