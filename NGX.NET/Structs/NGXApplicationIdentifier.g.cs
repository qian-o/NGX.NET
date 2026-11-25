namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXApplicationIdentifier
{
    public NGXApplicationIdentifierType IdentifierType;

    public NGXApplicationIdentifierUnion V;
}
