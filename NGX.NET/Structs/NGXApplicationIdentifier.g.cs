#nullable enable

namespace NGX.NET;

public struct NGXApplicationIdentifier
{
    public NGXApplicationIdentifierType IdentifierType;

    public NGXApplicationIdentifierUnion V;

    internal unsafe NGXApplicationIdentifier(in NGXApplicationIdentifierNative native)
    {
        IdentifierType = native.IdentifierType;
        V = native.IdentifierType is NGXApplicationIdentifierType.ProjectId ? new() { ProjectDesc = new NGXProjectIdDescription(in native.V.ProjectDesc) } : new() { ApplicationId = native.V.ApplicationId };
    }
}
