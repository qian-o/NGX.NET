namespace Streamline.NET.Generator;

/// <summary>
/// Reviewed parameter semantics that cannot be recovered from C++ pointer and reference types.
/// A changed function signature or parameter list requires an explicit review before generation.
/// </summary>
internal static class OverloadContracts
{
    // "ref" keeps the caller-initialized structure header and extension chain. ReturnValue marks
    // output structures that may also be initialized with new() for a throwing value overload.
    // "ref-address" is true in/out address replacement and must never become an output-only value.
    // References, spans and UTF-8 temporaries live for the call; frame tokens remain SDK-owned.

    private sealed record ParameterRule(string Name, string Convenience = "raw", string? CountParameter = null, bool ReturnValue = false);

    private sealed record FunctionRule(string Signature, ParameterRule[] Parameters);

    private static readonly Dictionary<string, FunctionRule> rules = new(StringComparer.Ordinal)
    {
        ["slInit"] = new("sl::Result (const sl::Preferences &, unsigned long long)",
            [new("pref", "in"), new("sdkVersion")]),
        ["slShutdown"] = new("sl::Result ()",
            []),
        ["slIsFeatureSupported"] = new("sl::Result (unsigned int, const sl::AdapterInfo &)",
            [new("feature"), new("adapterInfo", "in")]),
        ["slIsFeatureLoaded"] = new("sl::Result (unsigned int, bool &)",
            [new("feature"), new("loaded", "out")]),
        ["slSetFeatureLoaded"] = new("sl::Result (unsigned int, bool)",
            [new("feature"), new("loaded")]),
        ["slSetTagForFrame"] = new("sl::Result (const sl::FrameToken &, const sl::ViewportHandle &, const sl::ResourceTag *, unsigned int, void *)",
            [new("frame", "frame-token"), new("viewport", "in"), new("resources", "readonly-span", CountParameter: "numResources"), new("numResources"), new("cmdBuffer")]),
        ["slSetTag"] = new("sl::Result (const sl::ViewportHandle &, const sl::ResourceTag *, unsigned int, void *)",
            [new("viewport", "in"), new("tags", "readonly-span", CountParameter: "numTags"), new("numTags"), new("cmdBuffer")]),
        ["slSetConstants"] = new("sl::Result (const sl::Constants &, const sl::FrameToken &, const sl::ViewportHandle &)",
            [new("values", "in"), new("frame", "frame-token"), new("viewport", "in")]),
        ["slGetFeatureRequirements"] = new("sl::Result (unsigned int, sl::FeatureRequirements &)",
            [new("feature"), new("requirements", "ref", ReturnValue: true)]),
        ["slGetFeatureVersion"] = new("sl::Result (unsigned int, sl::FeatureVersion &)",
            [new("feature"), new("version", "ref", ReturnValue: true)]),
        ["slAllocateResources"] = new("sl::Result (void *, unsigned int, const sl::ViewportHandle &)",
            [new("cmdBuffer"), new("feature"), new("viewport", "in")]),
        ["slFreeResources"] = new("sl::Result (unsigned int, const sl::ViewportHandle &)",
            [new("feature"), new("viewport", "in")]),
        ["slEvaluateFeature"] = new("sl::Result (unsigned int, const sl::FrameToken &, const sl::BaseStructure **, unsigned int, void *)",
            [new("feature"), new("frame", "frame-token"), new("inputs"), new("numInputs"), new("cmdBuffer")]),
        ["slUpgradeInterface"] = new("sl::Result (void **)",
            [new("baseInterface", "ref-address")]),
        ["slGetNativeInterface"] = new("sl::Result (void *, void **)",
            [new("proxyInterface"), new("baseInterface", "out-address")]),
        ["slGetFeatureFunction"] = new("sl::Result (unsigned int, const char *, void *&)",
            [new("feature"), new("functionName", "utf8-string"), new("function", "out-address")]),
        ["slGetNewFrameToken"] = new("sl::Result (sl::FrameToken *&, const unsigned int *)",
            [new("token", "out-frame-token"), new("frameIndex")]),
        ["slSetD3DDevice"] = new("sl::Result (void *)",
            [new("d3dDevice")]),
        ["slDeepDVCSetOptions"] = new("sl::Result (const sl::ViewportHandle &, const sl::DeepDVCOptions &)",
            [new("viewport", "in"), new("options", "in")]),
        ["slDeepDVCGetState"] = new("sl::Result (const sl::ViewportHandle &, sl::DeepDVCState &)",
            [new("viewport", "in"), new("state", "ref", ReturnValue: true)]),
        ["slDirectSRGetOptimalSettings"] = new("sl::Result (const sl::DirectSROptions &, sl::DirectSROptimalSettings &)",
            [new("options", "in"), new("settings", "ref", ReturnValue: true)]),
        ["slDirectSRGetVariantInfo"] = new("sl::Result (unsigned int *, sl::DirectSRVariantInfo *)",
            [new("numVariants", "variant-query-or-fill"), new("variantInfo", "variant-query-or-fill")]),
        ["slDirectSRSetOptions"] = new("sl::Result (const sl::ViewportHandle &, const sl::DirectSROptions &)",
            [new("viewport", "in"), new("options", "in")]),
        ["slDLSSGetOptimalSettings"] = new("sl::Result (const sl::DLSSOptions &, sl::DLSSOptimalSettings &)",
            [new("options", "in"), new("settings", "ref", ReturnValue: true)]),
        ["slDLSSGetState"] = new("sl::Result (const sl::ViewportHandle &, sl::DLSSState &)",
            [new("viewport", "in"), new("state", "ref", ReturnValue: true)]),
        ["slDLSSSetOptions"] = new("sl::Result (const sl::ViewportHandle &, const sl::DLSSOptions &)",
            [new("viewport", "in"), new("options", "in")]),
        ["slDLSSDGetOptimalSettings"] = new("sl::Result (const sl::DLSSDOptions &, sl::DLSSDOptimalSettings &)",
            [new("options", "in"), new("settings", "ref", ReturnValue: true)]),
        ["slDLSSDGetState"] = new("sl::Result (const sl::ViewportHandle &, sl::DLSSDState &)",
            [new("viewport", "in"), new("state", "ref", ReturnValue: true)]),
        ["slDLSSDSetOptions"] = new("sl::Result (const sl::ViewportHandle &, const sl::DLSSDOptions &)",
            [new("viewport", "in"), new("options", "in")]),
        ["slDLSSGGetState"] = new("sl::Result (const sl::ViewportHandle &, sl::DLSSGState &, const sl::DLSSGOptions *)",
            [new("viewport", "in"), new("state", "ref", ReturnValue: true), new("options")]),
        ["slDLSSGSetOptions"] = new("sl::Result (const sl::ViewportHandle &, const sl::DLSSGOptions &)",
            [new("viewport", "in"), new("options", "in")]),
        ["slSetVulkanInfo"] = new("sl::Result (const sl::VulkanInfo &)",
            [new("info", "in")]),
        ["slNISSetOptions"] = new("sl::Result (const sl::ViewportHandle &, const sl::NISOptions &)",
            [new("viewport", "in"), new("options", "in")]),
        ["slNISGetState"] = new("sl::Result (const sl::ViewportHandle &, sl::NISState &)",
            [new("viewport", "in"), new("state", "ref", ReturnValue: true)]),
        ["slPCLGetState"] = new("sl::Result (sl::PCLState &)",
            [new("state", "ref", ReturnValue: true)]),
        ["slPCLSetMarker"] = new("sl::Result (sl::PCLMarker, const sl::FrameToken &)",
            [new("marker"), new("frame", "frame-token")]),
        ["slPCLSetOptions"] = new("sl::Result (const sl::PCLOptions &)",
            [new("options", "in")]),
        ["slReflexGetState"] = new("sl::Result (sl::ReflexState &)",
            [new("state", "ref", ReturnValue: true)]),
        ["slReflexSleep"] = new("sl::Result (const sl::FrameToken &)",
            [new("frame", "frame-token")]),
        ["slReflexSetOptions"] = new("sl::Result (const sl::ReflexOptions &)",
            [new("options", "in")]),
        ["slReflexSetCameraData"] = new("sl::Result (const sl::ViewportHandle &, const sl::FrameToken &, const sl::ReflexCameraData &)",
            [new("viewport", "in"), new("frame", "frame-token"), new("inCameraData", "in")]),
        ["slReflexGetPredictedCameraData"] = new("sl::Result (const sl::ViewportHandle &, const sl::FrameToken &, sl::ReflexPredictedCameraData &)",
            [new("viewport", "in"), new("frame", "frame-token"), new("outCameraData", "ref", ReturnValue: true)]),
    };

    public static void Apply(InterfaceSnapshot snapshot)
    {
        HashSet<string> applied = new(StringComparer.Ordinal);

        foreach (NativeDeclaration declaration in snapshot.Declarations.Where(declaration => declaration.Kind == "FUNCTION_DECL"
            && declaration.Name.StartsWith("sl", StringComparison.Ordinal) && !declaration.QualifiedName.Contains("::", StringComparison.Ordinal)))
        {
            if (!rules.TryGetValue(declaration.Name, out FunctionRule? rule))
            {
                throw new InvalidDataException("Review parameter semantics for the new SDK function: " + declaration.Name);
            }

            NativeDeclaration? alias = snapshot.Declarations.FirstOrDefault(item => item.Name == "PFun_" + declaration.Name);

            if (alias is null || alias.Type.Canonical != rule.Signature || alias.Type.CallingConvention != declaration.Type.CallingConvention)
            {
                throw new InvalidDataException("Review the missing or changed SDK function pointer alias: PFun_" + declaration.Name);
            }

            List<NativeDeclaration> parameters = [.. declaration.Parameters];

            if (declaration.Type.Canonical != rule.Signature || !parameters.Select(parameter => parameter.Name).SequenceEqual(rule.Parameters.Select(parameter => parameter.Name)))
            {
                throw new InvalidDataException("Review parameter semantics for the changed SDK function: " + declaration.Name);
            }

            for (int index = 0; index < parameters.Count; index++)
            {
                ParameterRule parameter = rule.Parameters[index];
                parameters[index].Contract = new()
                {
                    Convenience = parameter.Convenience,
                    CountParameter = parameter.CountParameter,
                    ReturnValue = parameter.ReturnValue
                };
            }

            applied.Add(declaration.Name);
        }

        foreach (string name in rules.Keys.Except(applied, StringComparer.Ordinal))
        {
            throw new InvalidDataException("Remove or update the contract for the missing SDK function: " + name);
        }
    }
}
