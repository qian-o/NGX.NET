using System.Runtime.InteropServices;
using NGX.NET;
using Showcase.Models;
using Ngx = NGX.NET.NGX;

namespace Showcase;

internal sealed unsafe partial class NGXSession : IDisposable
{
    public Dictionary<NGXFeature, string> Unavailable { get; } = [];

    public bool IsVulkan { get; private set; }

    private nint device;
    private NGXParameter* capabilities;
    private NGXParameter* parameters;
    private NGXParameter* frameParameters;
    private NGXHandle* reconstruction;
    private NGXHandle* generation;
    private RenderSettings settings = new();
    private int inputWidth, inputHeight, outputWidth, outputHeight;
    private bool initialized;
    private void* runtimePath;
    private void* dataPath;
    private sbyte* projectId;
    private sbyte* engineVersion;
    private void** paths;

    public NGXSession()
    {
        string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        try
        {
            runtimePath = NGXMarshal.StringToPtr(Ngx.RuntimeDirectory, NGXEncoding.NativeWide);
            dataPath = NGXMarshal.StringToPtr(logDirectory, NGXEncoding.NativeWide);
            projectId = (sbyte*)NGXMarshal.StringToPtr("fc6ac847-10b0-48e1-842d-1bc819f8d2f4", NGXEncoding.Utf8);
            engineVersion = (sbyte*)NGXMarshal.StringToPtr("NGX.NET.Showcase.1.0", NGXEncoding.Utf8);
            paths = (void**)NativeMemory.Alloc((nuint)sizeof(nint));
        }
        catch
        {
            NativeMemory.Free(paths);
            NGXMarshal.Free(engineVersion);
            NGXMarshal.Free(projectId);
            NGXMarshal.Free(dataPath);
            NGXMarshal.Free(runtimePath);

            throw;
        }
    }

    public bool Available(NGXFeature feature) => !Unavailable.ContainsKey(feature);

    public void Initialize(nint nativeDevice, nint instance = 0, nint physical = 0, nint getInstanceProcAddr = 0, nint getDeviceProcAddr = 0)
    {
        IsVulkan = instance != 0;
        device = nativeDevice;
        *paths = runtimePath;
        NGXFeatureCommonInfo common = new()
        {
            PathListInfo = new()
            {
                Path = paths,
                Length = 1
            }
        };

        NGXResult result = IsVulkan
            ? Ngx.Vulkan.InitWithProjectID(projectId, NGXEngineType.CUSTOM, engineVersion, dataPath, instance, physical, device,
                (delegate* unmanaged[Cdecl]<nint, sbyte*, delegate* unmanaged[Cdecl]<void>>)getInstanceProcAddr,
                (delegate* unmanaged[Cdecl]<nint, sbyte*, delegate* unmanaged[Cdecl]<void>>)getDeviceProcAddr, &common, (NGXVersion)Ngx.VersionAPI)
            : Ngx.D3D12.InitWithProjectID(projectId, NGXEngineType.CUSTOM, engineVersion, dataPath, device, &common, (NGXVersion)Ngx.VersionAPI);

        if (result is NGXResult.FAILFeatureNotSupported or NGXResult.FAILPlatformError or NGXResult.FAILOutOfDate)
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
        NGXParameter* allocated = null;
        Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.GetCapabilityParameters(&allocated) : Ngx.D3D12.GetCapabilityParameters(&allocated));
        capabilities = allocated;
        allocated = null;
        Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.AllocateParameters(&allocated) : Ngx.D3D12.AllocateParameters(&allocated));
        parameters = allocated;
        allocated = null;
        Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.AllocateParameters(&allocated) : Ngx.D3D12.AllocateParameters(&allocated));
        frameParameters = allocated;

        Query(NGXFeature.SuperSampling, Ngx.ParameterSuperSamplingAvailable);
        Query(NGXFeature.RayReconstruction, Ngx.ParameterSuperSamplingDenoisingAvailable);
        Query(NGXFeature.FrameGeneration, Ngx.ParameterFrameGenerationAvailable);
    }

    public string[] VulkanExtensions(nint instance = 0, nint physical = 0)
    {
        HashSet<string> extensions = [];
        *paths = runtimePath;
        NGXFeatureCommonInfo common = new()
        {
            PathListInfo = new()
            {
                Path = paths,
                Length = 1
            }
        };

        foreach (NGXFeature feature in new[] { NGXFeature.SuperSampling, NGXFeature.RayReconstruction, NGXFeature.FrameGeneration })
        {
            NGXFeatureDiscoveryInfo discovery = new()
            {
                SDKVersion = (NGXVersion)Ngx.VersionAPI,
                FeatureID = feature,
                Identifier = new()
                {
                    IdentifierType = NGXApplicationIdentifierType.ProjectId,
                    V = new()
                    {
                        ProjectDesc = new()
                        {
                            ProjectId = projectId,
                            EngineType = NGXEngineType.CUSTOM,
                            EngineVersion = engineVersion
                        }
                    }
                },
                ApplicationDataPath = dataPath,
                FeatureInfo = &common
            };

            uint count = 0;
            NGXVkExtensionProperties* properties = null;
            NGXResult result = instance == 0
                ? Ngx.Vulkan.GetFeatureInstanceExtensionRequirements(&discovery, &count, &properties)
                : Ngx.Vulkan.GetFeatureDeviceExtensionRequirements(instance, physical, &discovery, &count, &properties);

            if (Ngx.Failed(result))
            {
                Unavailable[feature] = $"Extension requirements: {result}";
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                extensions.Add(NGXMarshal.PtrToString(properties[i].ExtensionName, NGXEncoding.Utf8)!);
            }
        }

        return [.. extensions];
    }

    private void Query(NGXFeature feature, string name)
    {
        int available = 0;
        NGXResult result;
        void* key = NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);

        try
        {
            result = Ngx.Parameter.GetI(capabilities, (sbyte*)key, &available);
        }
        finally
        {
            NGXMarshal.Free(key);
        }

        if (Ngx.Failed(result) || available == 0)
        {
            Unavailable[feature] = Ngx.Failed(result) ? result.ToString() : "Not supported by this device/driver";
        }

        Console.WriteLine($"{feature}: {(Available(feature) ? "Available" : Unavailable[feature])}");
    }

    // The caller completes submitted GPU work before reconfiguration or disposal.
    public void ReleaseReconstruction() => Release(ref reconstruction);

    public void ReleaseFrameGeneration() => Release(ref generation);

    private void Release(ref NGXHandle* handle)
    {
        if (handle != null)
        {
            Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.ReleaseFeature(handle) : Ngx.D3D12.ReleaseFeature(handle));
            handle = null;
        }
    }

    public void Dispose()
    {
        List<Exception> failures = [];

        void Check(NGXResult result, string operation)
        {
            if (Ngx.Failed(result))
            {
                failures.Add(new NGXException(result, operation));
            }
        }

        if (initialized)
        {
            // Complete shutdown even when an individual native release fails.
            foreach (nint value in new nint[] { (nint)reconstruction, (nint)generation })
            {
                if (value != 0)
                {
                    Check(IsVulkan ? Ngx.Vulkan.ReleaseFeature((NGXHandle*)value) : Ngx.D3D12.ReleaseFeature((NGXHandle*)value), "ReleaseFeature");
                }
            }

            reconstruction = generation = null;

            foreach (nint value in new nint[] { (nint)frameParameters, (nint)parameters, (nint)capabilities })
            {
                if (value != 0)
                {
                    Check(IsVulkan ? Ngx.Vulkan.DestroyParameters((NGXParameter*)value) : Ngx.D3D12.DestroyParameters((NGXParameter*)value), "DestroyParameters");
                }
            }

            frameParameters = parameters = capabilities = null;
            Check(IsVulkan ? Ngx.Vulkan.Shutdown1(device) : Ngx.D3D12.Shutdown1(device), "Shutdown1");
            initialized = false;
        }

        NativeMemory.Free(paths);
        paths = null;
        NGXMarshal.Free(runtimePath);
        runtimePath = null;
        NGXMarshal.Free(dataPath);
        dataPath = null;
        NGXMarshal.Free(projectId);
        projectId = null;
        NGXMarshal.Free(engineVersion);
        engineVersion = null;

        if (failures.Count != 0)
        {
            throw new AggregateException("NGX shutdown failed.", failures);
        }
    }
}
