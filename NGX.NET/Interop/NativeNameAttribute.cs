namespace NGX.NET;

/// <summary>
/// Identifies the official declaration represented by a managed type or field.
/// </summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class NativeNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Name in the public NGX or referenced Vulkan headers.
    /// </summary>
    public string Name { get; } = name;
}
