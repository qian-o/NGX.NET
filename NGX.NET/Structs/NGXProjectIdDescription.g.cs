#nullable enable

namespace NGX.NET;

public struct NGXProjectIdDescription
{
    public string? ProjectId;

    public NGXEngineType EngineType;

    public string? EngineVersion;

    internal unsafe NGXProjectIdDescription(in NGXProjectIdDescriptionNative native)
    {
        ProjectId = NGXMarshal.PtrToString(native.ProjectId, NGXEncoding.Utf8);
        EngineType = native.EngineType;
        EngineVersion = NGXMarshal.PtrToString(native.EngineVersion, NGXEncoding.Utf8);
    }
}
