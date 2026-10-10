#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 32)]
internal unsafe struct NGXApplicationIdentifierNative
{
    [FieldOffset(0)]
    public NGXApplicationIdentifierType IdentifierType;

    [FieldOffset(8)]
    public NGXApplicationIdentifierUnionNative V;

    public NGXApplicationIdentifierNative(in NGXApplicationIdentifier value, NativeScope scope)
    {
        if ((value.IdentifierType is NGXApplicationIdentifierType.ProjectId) != value.V.ProjectDesc.HasValue)
        {
            throw new ArgumentException("Application identifier and active union member disagree.", nameof(value));
        }

        if (value.IdentifierType is not (NGXApplicationIdentifierType.ProjectId or NGXApplicationIdentifierType.ApplicationId))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        IdentifierType = value.IdentifierType;
        V = new(in value.V, scope);
    }
}
