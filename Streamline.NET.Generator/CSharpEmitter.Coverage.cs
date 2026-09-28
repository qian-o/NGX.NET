using System.Security.Cryptography;
using System.Text;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private static string SourceHash(string source)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private bool TryRecordManual(NativeDeclaration declaration)
    {
        if (!ReviewedImplementations.Declarations.TryGetValue(declaration.Id, out ReviewedImplementations.Implementation? reviewed))
        {
            return false;
        }

        if (SourceHash(declaration.Source) != reviewed.NativeSourceSha256)
        {
            throw new InvalidDataException($"The upstream body changed; review the implementation of {declaration.QualifiedName} in {reviewed.File}.");
        }

        if (reviewed.File is null || !System.IO.File.Exists(Path.Combine(outputDirectory, reviewed.File)))
        {
            throw new InvalidDataException("Missing reviewed implementation file: " + reviewed.File);
        }

        Record(declaration, reviewed.Description);

        if (declaration.Kind == "CLASS_TEMPLATE")
        {
            // The reviewed class body covers the explicit storage, member and generic
            // constraints together. Its source hash changes for any member addition.
            foreach (NativeDeclaration child in Descendants(declaration))
            {
                Record(child, reviewed.Description + " / " + child.Name);
            }
        }

        return true;
    }

    private static IEnumerable<NativeDeclaration> Descendants(NativeDeclaration declaration)
    {
        foreach (NativeDeclaration child in declaration.Children)
        {
            yield return child;

            foreach (NativeDeclaration descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private void CompleteCoverage(NativeDeclaration declaration, string? parentImplementation = null)
    {
        implementations.TryGetValue(declaration.Id, out string? implementation);

        if (implementation is null && TryRecordManual(declaration))
        {
            implementation = implementations[declaration.Id];
        }

        if (implementation is null && parentImplementation?.StartsWith("Excluded:", StringComparison.Ordinal) == true)
        {
            implementation = parentImplementation;
            Record(declaration, implementation);
        }

        if (implementation is null && parentImplementation is not null && declaration.Kind is "PARM_DECL" or "TEMPLATE_TYPE_PARAMETER")
        {
            implementation = "Parameter/generic contract in " + parentImplementation;
            Record(declaration, implementation);
        }

        if (implementation is null)
        {
            unhandled.Add(declaration.Id + ": " + declaration.QualifiedName + " (" + declaration.Kind + ")");
        }

        foreach (NativeDeclaration child in declaration.Children)
        {
            CompleteCoverage(child, implementation);
        }
    }

    private void CheckMacros()
    {
        foreach (NativeMacro macro in snapshot.Macros)
        {
            if (macro.Classification is not ("application" or "compilation" or "implementation"))
            {
                unhandled.Add("Unclassified macro: " + macro.Name);
            }

            if (!ReviewedImplementations.Macros.TryGetValue(macro.Id, out ReviewedImplementations.Implementation? reviewed) || reviewed.NativeSourceSha256 != SourceHash(macro.Body))
            {
                unhandled.Add("Unreviewed macro: " + macro.Name + " in " + macro.File);
            }
            else
            {
                implementations[macro.Id] = reviewed.Description;
            }
        }
    }

    private void RecordCallbackAliases()
    {
        IEnumerable<NativeDeclaration> fields = snapshot.Declarations.SelectMany(Descendants).Where(declaration => declaration.Kind == "FIELD_DECL");

        foreach (NativeDeclaration alias in snapshot.Declarations.Where(declaration => declaration.Kind is "TYPE_ALIAS_DECL" or "TYPEDEF_DECL"))
        {
            if (implementations.ContainsKey(alias.Id))
            {
                continue;
            }

            NativeDeclaration? field = fields.FirstOrDefault(field => field.Type.Element?.Canonical == alias.Type.Canonical);

            if (field is not null && alias.Type.Kind == "FUNCTIONPROTO")
            {
                Record(alias, "Unmanaged callback signature in " + field.QualifiedName);
            }
        }
    }

    private void EmitPublicMacros()
    {
        StringBuilder builder = File("Core", "SL.Macros");
        builder.AppendLine();

        foreach ((string native, string managed) in new[] { ("SL_VERSION_MAJOR", "VersionMajor"), ("SL_VERSION_MINOR", "VersionMinor"), ("SL_VERSION_PATCH", "VersionPatch") })
        {
            NativeMacro macro = snapshot.Macros.Single(item => item.Name == native);

            if (!int.TryParse(macro.Body, out int value))
            {
                throw new InvalidDataException("Unevaluated version macro: " + native);
            }

            builder.AppendLine($"    /// <summary>Native {native} value.</summary>");
            builder.AppendLine($"    public const int {managed} = {value};");
            builder.AppendLine();
        }

        NativeMacro bufferId = snapshot.Macros.Single(item => item.Name == "FEATURE_SPECIFIC_BUFFER_TYPE_ID");

        if (bufferId.Body != "(feature, number) feature << 16 | number")
        {
            throw new InvalidDataException("Review the feature-specific buffer ID expression.");
        }

        builder.AppendLine("    /// <summary>Evaluates the native feature-specific buffer ID expression once per argument.</summary>");
        builder.AppendLine("    public static uint FeatureSpecificBufferTypeId(uint feature, uint number) => (feature << 16) | number;");
    }
}
