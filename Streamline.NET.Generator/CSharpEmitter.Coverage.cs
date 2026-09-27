using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private sealed class ReviewedImplementation
    {
        public string Id { get; set; } = "";

        public string NativeSourceSha256 { get; set; } = "";

        public string? File { get; set; }

        public string Implementation { get; set; } = "";
    }

    private sealed class ReviewedImplementations
    {
        public List<ReviewedImplementation> Declarations { get; set; } = [];

        public List<ReviewedImplementation> Macros { get; set; } = [];
    }

    private readonly Dictionary<string, ReviewedImplementation> manualDeclarations = new(StringComparer.Ordinal);

    private readonly Dictionary<string, ReviewedImplementation> reviewedMacros = new(StringComparer.Ordinal);

    private static string SourceHash(string source)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private void LoadReviewedImplementations()
    {
        string path = Path.Combine(Path.GetDirectoryName(outputDirectory)!, "Streamline.NET.Generator", "ManualImplementations.json");
        ReviewedImplementations reviewed = JsonSerializer.Deserialize<ReviewedImplementations>(System.IO.File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        foreach (ReviewedImplementation item in reviewed.Declarations)
        {
            manualDeclarations.Add(item.Id, item);
        }
        foreach (ReviewedImplementation item in reviewed.Macros)
        {
            reviewedMacros.Add(item.Id, item);
        }
    }

    private bool TryRecordManual(NativeDeclaration declaration)
    {
        if (!manualDeclarations.TryGetValue(declaration.Id, out ReviewedImplementation? reviewed))
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
        Record(declaration, reviewed.Implementation);
        if (declaration.Kind == "CLASS_TEMPLATE")
        {
            // The reviewed class body covers the explicit storage, member and generic
            // constraints together. Its source hash changes for any member addition.
            foreach (NativeDeclaration child in Descendants(declaration))
            {
                Record(child, reviewed.Implementation + " / " + child.Name);
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
            if (!reviewedMacros.TryGetValue(macro.Id, out ReviewedImplementation? reviewed) || reviewed.NativeSourceSha256 != SourceHash(macro.Body))
            {
                unhandled.Add("Unreviewed macro: " + macro.Name + " in " + macro.File);
            }
            else
            {
                implementations[macro.Id] = reviewed.Implementation;
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

    private void WriteCoverage()
    {
        string path = Path.Combine(Path.GetDirectoryName(outputDirectory)!, "Streamline.NET.Generator", "declaration-coverage.json");
        Dictionary<string, string> previous = new(StringComparer.Ordinal);
        if (System.IO.File.Exists(path))
        {
            using JsonDocument document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            foreach (JsonElement item in document.RootElement.GetProperty("declarations").EnumerateArray())
            {
                previous[item.GetProperty("id").GetString()!] = item.GetProperty("fingerprint").GetString()!;
            }
        }
        var entries = snapshot.Declarations.SelectMany(declaration => new[] { declaration }.Concat(Descendants(declaration)))
            .Select(declaration => new
            {
                id = declaration.Id,
                name = declaration.QualifiedName,
                source = declaration.File + ":" + declaration.Line,
                classification = declaration.Classification,
                fingerprint = SourceHash(JsonSerializer.Serialize(declaration)),
                implementation = implementations[declaration.Id]
            }).OrderBy(entry => entry.id, StringComparer.Ordinal).ToArray();
        int added = entries.Count(entry => !previous.ContainsKey(entry.id));
        int changed = entries.Count(entry => previous.TryGetValue(entry.id, out string? fingerprint) && fingerprint != entry.fingerprint);
        int removed = previous.Keys.Except(entries.Select(entry => entry.id), StringComparer.Ordinal).Count();
        Console.WriteLine($"Declaration changes: {added} added, {changed} changed, {removed} removed.");
        string content = JsonSerializer.Serialize(new
        {
            source = snapshot.Source,
            unclassified = 0,
            unhandled = 0,
            declarations = entries,
            macros = snapshot.Macros.Select(macro => new { id = macro.Id, name = macro.Name, classification = macro.Classification, implementation = implementations[macro.Id] })
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n";
        if (!System.IO.File.Exists(path) || System.IO.File.ReadAllText(path) != content)
        {
            System.IO.File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }
}
