namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private readonly HashSet<string> featureGroups = [.. snapshot.Declarations
        .Where(declaration => declaration.Kind == "FUNCTION_DECL"
            && declaration.Name.StartsWith("sl", StringComparison.Ordinal)
            && !declaration.QualifiedName.Contains("::", StringComparison.Ordinal)
            && !snapshot.Exports.Contains(declaration.Name, StringComparer.Ordinal))
        .Select(TypeMapper.Group)];

    private string FunctionName(NativeDeclaration declaration)
    {
        string name = declaration.Name[2..];
        if (snapshot.Exports.Contains(declaration.Name, StringComparer.Ordinal))
        {
            return name;
        }
        string feature = TypeMapper.Group(declaration);
        if (!featureGroups.Contains(feature) || !name.StartsWith(feature, StringComparison.Ordinal) || name.Length == feature.Length)
        {
            throw new InvalidDataException("Unresolved feature method name: " + declaration.Name);
        }
        return name[feature.Length..];
    }

    private string HelperGroup(NativeDeclaration declaration)
    {
        string group = TypeMapper.Group(declaration);
        if (featureGroups.Contains(group))
        {
            return group;
        }
        foreach (NativeDeclaration parameter in declaration.Parameters)
        {
            NativeDeclaration? type = snapshot.Declarations.FirstOrDefault(item => item.QualifiedName == parameter.Type.Declaration);
            if (type is not null && featureGroups.Contains(TypeMapper.Group(type)))
            {
                return TypeMapper.Group(type);
            }
        }
        return group;
    }

    private string HelperName(NativeDeclaration declaration)
    {
        string name = TypeMapper.PascalCase(declaration.Name);
        string group = HelperGroup(declaration);
        if (featureGroups.Contains(group))
        {
            foreach (string verb in new[] { "Get", "Resolve" })
            {
                if (name.StartsWith(verb + group, StringComparison.Ordinal))
                {
                    return verb + name[(verb.Length + group.Length)..];
                }
            }
        }
        return name;
    }

    private string ApiPath(string group, string name)
    {
        return featureGroups.Contains(group) ? "SL." + group + "." + name : "SL." + name;
    }
}
