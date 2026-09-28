using System.Text;
using System.Text.RegularExpressions;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private static string VulkanConstantName(string nativeName)
    {
        return "Vk" + string.Concat(nativeName[3..].Split('_').Select(part => part is "NV" or "KHR" or "EXT" ? part : TypeMapper.PascalCase(part.ToLowerInvariant())));
    }

    private void EmitVulkanHelper(NativeDeclaration declaration)
    {
        StringBuilder builder = File("Vulkan", "SL.Helpers");
        string name = TypeMapper.PascalCase(declaration.Name);
        builder.AppendLine();
        Comment(builder, declaration, "    ");

        if (declaration.Name == "getMergedSupportedVkPhysicalDeviceVulkanFeatures")
        {
            builder.AppendLine($"    public static void {name}(VkBaseOutStructure* physicalDeviceFeatures, VkBaseOutStructure* featuresToMerge, VkBaseOutStructure* supportedFeatures)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (physicalDeviceFeatures == null || supportedFeatures == null)");
            builder.AppendLine("        {");
            builder.AppendLine("            return;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        System.Diagnostics.Debug.Assert(featuresToMerge == null || featuresToMerge->SType == physicalDeviceFeatures->SType);");
            builder.AppendLine("        System.Diagnostics.Debug.Assert(supportedFeatures->SType == physicalDeviceFeatures->SType);");
            builder.AppendLine("        switch (physicalDeviceFeatures->SType)");
            builder.AppendLine("        {");
            MatchCollection cases = Regex.Matches(declaration.Source, @"case\s+(VK_STRUCTURE_TYPE_\w+):([\s\S]*?)break;");

            if (cases.Count == 0)
            {
                throw new InvalidDataException("Missing Vulkan feature merge cases.");
            }

            foreach (Match branch in cases)
            {
                builder.AppendLine($"            case {VulkanConstantName(branch.Groups[1].Value)}:");
                builder.AppendLine("                {");
                MatchCollection fields = Regex.Matches(branch.Groups[2].Value, @"SL_VK_FEATURE_MERGE_SUPPORT\((\w+),\s*(\w+)\)");
                string type = fields[0].Groups[1].Value;
                builder.AppendLine($"                    {type}* destination = ({type}*)physicalDeviceFeatures;");
                builder.AppendLine($"                    {type}* additions = ({type}*)featuresToMerge;");
                builder.AppendLine($"                    {type}* supported = ({type}*)supportedFeatures;");
                builder.AppendLine();

                foreach (Match field in fields)
                {
                    string member = TypeMapper.MemberName(field.Groups[2].Value);
                    // Upstream && and || produce VK_TRUE/VK_FALSE, not a bitwise intersection.
                    builder.AppendLine($"                    destination->{member} = ((destination->{member} != 0 || (additions != null && additions->{member} != 0)) && supported->{member} != 0) ? 1u : 0u;");
                }

                builder.AppendLine();
                builder.AppendLine("                    break;");
                builder.AppendLine("                }");
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
        }
        else
        {
            string result = mapper.Map(declaration.ResultType!);
            Match constant = Regex.Match(declaration.Source, @"features\{\s*(VK_STRUCTURE_TYPE_\w+)\s*\}");
            MatchCollection fields = Regex.Matches(declaration.Source, @"SL_VK_FEATURE\((\w+)\)");

            if (!constant.Success || fields.Count == 0)
            {
                throw new InvalidDataException("Unsupported Vulkan feature-name helper: " + declaration.Name);
            }

            builder.AppendLine($"    public static {result} {name}(uint featureCount, sbyte** featureNames)");
            builder.AppendLine("    {");
            builder.AppendLine($"        {result} features = new()");
            builder.AppendLine("        {");
            builder.AppendLine($"            SType = {VulkanConstantName(constant.Groups[1].Value)}");
            builder.AppendLine("        };");
            builder.AppendLine("        for (uint index = 0; index < featureCount; index++)");
            builder.AppendLine("        {");

            foreach (Match field in fields)
            {
                string nativeName = field.Groups[1].Value;
                builder.AppendLine($"            if (NativeStrings.Equals(featureNames[index], \"{nativeName}\"u8))");
                builder.AppendLine("            {");
                builder.AppendLine($"                features.{TypeMapper.MemberName(nativeName)} = 1;");
                builder.AppendLine("            }");
            }

            builder.AppendLine("        }");
            builder.AppendLine("        return features;");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    /// <summary>Encodes names as temporary UTF-8 strings and invokes the pointer helper.</summary>");
            builder.AppendLine($"    public static {result} {name}(ReadOnlySpan<string> featureNames)");
            builder.AppendLine("    {");
            builder.AppendLine("        using Utf8StringArray names = new(featureNames);");
            builder.AppendLine($"        return {name}((uint)featureNames.Length, names.Pointer);");
            builder.AppendLine("    }");
        }

        Record(declaration, "SL." + name);
    }
}
