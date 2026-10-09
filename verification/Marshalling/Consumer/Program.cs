using NGX.NET;

// Compiles with unsafe disabled. Native entry points are deliberately not run
// on a host without the NVIDIA runtime; the method checks the public signatures.
NGXFeatureCommonInfo common = new()
{
    PathListInfo = new()
    {
        Paths = ["/runtime/中文🚀"]
    }
};
NGXDLSSCreateParams create = new()
{
    Feature = new()
    {
        InWidth = 1280,
        InHeight = 720,
        InTargetWidth = 1920,
        InTargetHeight = 1080,
        InPerfQualityValue = NGXPerfQualityValue.MaxQuality
    }
};
Action invoke = () =>
{
    Ngx.D3D12.InitWithProjectID("project", NGXEngineType.Custom, "1.0", "/tmp", 1, in common, NGXVersion.Api);
    Ngx.D3D12.AllocateParameters(out NGXParameter parameters);
    Ngx.Parameter.SetUI(parameters, Ngx.ParameterWidth, 1280);
    Ngx.Parameter.GetI(parameters, Ngx.ParameterSuperSamplingAvailable, out int available);
    Ngx.D3D12.CreateDLSSExt(1, 1, 1, out NGXHandle handle, parameters, in create);
    Ngx.D3D12.ReleaseFeature(handle);
    Ngx.D3D12.DestroyParameters(parameters);
    Ngx.D3D12.Shutdown1(1);
};
GC.KeepAlive(invoke);
Console.WriteLine("PASS safe consumer: production public API compiles with unsafe disabled.");
return 0;
