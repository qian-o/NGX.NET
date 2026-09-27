using System.Globalization;
using System.Numerics;
using ImGuiNET;
using Streamline.NET;

namespace Showcase;

internal sealed unsafe partial class UserInterface : IDisposable
{
    private readonly nint context;
    private readonly bool chinese;
    private bool visible = true;
    private float uiScale = 1;
    private const float TextSize = 20;
    private const float AtlasTextSize = 40;
    private static readonly Vector4 Muted = new(0.65f, 0.69f, 0.73f, 1);
    private static readonly Vector4 Accent = new(0.9f, 0.77f, 0.51f, 1);
    public byte[] FontPixels
    {
        get;
    }
    public int FontWidth
    {
        get;
    }
    public int FontHeight
    {
        get;
    }
    public bool ResetCamera;

    public UserInterface()
    {
        context = ImGui.CreateContext();
        ImGuiIOPtr io = ImGui.GetIO();
        io.NativePtr->IniFilename = null;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        ImGui.StyleColorsDark();
        ImGuiStylePtr style = ImGui.GetStyle();
        style.WindowRounding = 8;
        style.FrameRounding = 4;
        style.WindowPadding = new(18, 16);
        style.FramePadding = new(10, 6);
        style.ItemSpacing = new(10, 10);
        style.Colors[(int)ImGuiCol.WindowBg] = new(0.04f, 0.05f, 0.065f, 0.97f);
        style.Colors[(int)ImGuiCol.Border] = new(0.22f, 0.25f, 0.29f, 1);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new(0.13f, 0.16f, 0.20f, 1);
        style.Colors[(int)ImGuiCol.FrameBg] = new(0.1f, 0.13f, 0.16f, 1);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new(0.18f, 0.22f, 0.26f, 1);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new(0.22f, 0.26f, 0.30f, 1);
        style.Colors[(int)ImGuiCol.Header] = new(0.19f, 0.22f, 0.25f, 1);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new(0.27f, 0.30f, 0.33f, 1);
        style.Colors[(int)ImGuiCol.Button] = new(0.19f, 0.23f, 0.27f, 1);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new(0.28f, 0.33f, 0.37f, 1);
        style.Colors[(int)ImGuiCol.CheckMark] = Accent;
        style.Colors[(int)ImGuiCol.SliderGrab] = Accent;

        string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string? cjk = new[] { "msyh.ttc", "msjh.ttc", "simhei.ttf", "simsun.ttc" }
            .Select(name => Path.Combine(fonts, name)).FirstOrDefault(File.Exists);
        chinese = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" && cjk is not null;
        string? fontPath = chinese ? cjk : new[] { "segoeui.ttf", "arial.ttf" }
            .Select(name => Path.Combine(fonts, name)).FirstOrDefault(File.Exists);
        ImFontConfigPtr config = ImGuiNative.ImFontConfig_ImFontConfig();
        ImFontGlyphRangesBuilderPtr builder = ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();
        ImVector ranges = default;
        try
        {
            config.SizePixels = AtlasTextSize;
            config.OversampleH = 2;
            config.OversampleV = 1;
            builder.AddRanges(io.Fonts.GetGlyphRangesDefault());
            builder.AddText("→…—");
            if (chinese)
            {
                foreach (string text in ChineseText.Values)
                {
                    builder.AddText(text);
                }
            }
            builder.BuildRanges(out ranges);
            if (fontPath is not null)
            {
                io.Fonts.AddFontFromFileTTF(fontPath, config.SizePixels, config, ranges.Data);
            }
            else
            {
                io.Fonts.AddFontDefault(config);
            }
            // Glyph ranges are borrowed by ImGui and must live through atlas construction.
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
            FontWidth = width;
            FontHeight = height;
            FontPixels = new ReadOnlySpan<byte>(pixels, width * height * bytesPerPixel).ToArray();
        }
        finally
        {
            if (ranges.Data != 0)
            {
                ImGui.MemFree(ranges.Data);
            }

            builder.Destroy();
            config.Destroy();
        }
        io.Fonts.SetTexID(1);
        io.FontGlobalScale = TextSize / AtlasTextSize;
    }

    public void Build(RHI rhi, float delta)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new(rhi.Window.Width, rhi.Window.Height);
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = Math.Max(delta, 1e-4f);
        float scale = rhi.Window.DpiScale;
        bool scaleChanged = scale != uiScale;
        if (scaleChanged)
        {
            ImGui.GetStyle().ScaleAllSizes(scale / uiScale);
            uiScale = scale;
        }
        io.FontGlobalScale = TextSize / AtlasTextSize * uiScale;
        ImGui.NewFrame();
        if (ImGui.IsKeyPressed(ImGuiKey.F1, false))
        {
            visible = !visible;
        }

        if (!visible)
        {
            ImGui.SetNextWindowPos(new Vector2(18) * uiScale, ImGuiCond.Always);
            if (ImGui.Begin("##ShowControls", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                if (ImGui.Button(T("Show settings (F1)")))
                {
                    visible = true;
                }
            }
            ImGui.End();
            ImGui.Render();
            return;
        }

        Vector2 available = Vector2.Max(new(1), io.DisplaySize - new Vector2(36) * uiScale);
        ImGui.SetNextWindowPos(new Vector2(18) * uiScale, ImGuiCond.Always);
        ImGui.SetNextWindowSize(Vector2.Min(new Vector2(420, 860) * uiScale, available), scaleChanged ? ImGuiCond.Always : ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(Vector2.Min(new Vector2(280, 240) * uiScale, available), available);
        if (ImGui.Begin(T("Picture settings") + "###ShowcaseControls", ref visible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove))
        {
            HeaderLine("SPONZA", $"{T("Natural daylight")} / {rhi.BackendName}", Accent);
            string fps = rhi.RenderFps > 0 ? rhi.RenderFps.ToString("F0", CultureInfo.InvariantCulture) : "--";
            HeaderLine($"{T("Real frames")}: {fps} FPS", rhi.GpuMilliseconds is double gpu ? $"GPU {gpu:F1} ms" : T("Measuring..."), Vector4.One);
            Help("Display FPS is not measured; no value is inferred from the frame multiplier.");
            ImGui.PushItemWidth(-1);
            QuickSetup(rhi);
            Picture(rhi);
            if (ImGui.CollapsingHeader(T("Camera and comparison")))
            {
                CameraControls(rhi.Settings);
            }

            if (ImGui.CollapsingHeader(T("Advanced options")))
            {
                Advanced(rhi);
            }

            if (ImGui.CollapsingHeader(T("Device and measurements")))
            {
                Information(rhi);
            }

            ImGui.PopItemWidth();
            ImGui.Spacing();
            Hint("Right mouse + WASD: move / F1: panel");
        }
        ImGui.End();
        ImGui.Render();
    }

    private void QuickSetup(RHI rhi)
    {
        ImGui.SeparatorText(T("Quick setup"));
        QualityPreset? current = QualityPresets.Match(rhi.Settings, rhi.Capabilities);
        if (ImGui.BeginCombo("##Preset", current is QualityPreset selected ? PresetLabel(selected) : T("Custom")))
        {
            foreach (QualityPreset preset in Enum.GetValues<QualityPreset>())
            {
                if (ImGui.Selectable(PresetLabel(preset), current == preset))
                {
                    QualityPresets.Apply(rhi.Settings, preset, rhi.Capabilities);
                }

                Help(PresetDescription(preset));
            }
            ImGui.EndCombo();
        }
        if (current is QualityPreset active)
        {
            Hint(PresetDescription(active));
        }

        Help("Presets adapt to the features available on this device.");
    }

    private void Picture(RHI rhi)
    {
        RenderSettings settings = rhi.Settings;
        StreamlineSession session = rhi.Streamline;
        ImGui.SeparatorText(T("Current picture"));
        ImGui.PushStyleColor(ImGuiCol.Text, Accent);
        ImGui.TextWrapped(settings.RayTracing ? settings.Reconstruction == Reconstruction.RayReconstruction ? T("Ray tracing + Ray Reconstruction") : T("Raw ray-traced input") : T("Raster lighting"));
        ImGui.PopStyleColor();
        bool denoised = settings.RayTracing && settings.Reconstruction == Reconstruction.RayReconstruction;
        ImGui.BeginDisabled(!rhi.Capabilities.RayReconstruction);
        if (ImGui.Checkbox(T("Ray tracing and denoising"), ref denoised))
        {
            settings.RayTracing = denoised;
            settings.Reconstruction = denoised ? Reconstruction.RayReconstruction : rhi.Capabilities.Dlss ? Reconstruction.DLSS : Reconstruction.Native;
        }
        Help(rhi.Capabilities.RayReconstruction ? "Uses real reflected and bounced light. Ray Reconstruction removes sampling noise." : "This combination needs hardware ray queries and DLSS Ray Reconstruction.");
        ImGui.EndDisabled();
        if (settings.Reconstruction is Reconstruction.DLSS or Reconstruction.RayReconstruction)
        {
            ImGui.TextUnformatted(T("Image quality"));
            if (ImGui.BeginCombo("##Quality", QualityLabel(settings.Quality)))
            {
                foreach (DLSSMode mode in new[] { DLSSMode.MaxQuality, DLSSMode.Balanced, DLSSMode.MaxPerformance, DLSSMode.UltraPerformance, DLSSMode.DLAA })
                {
                    if (ImGui.Selectable(QualityLabel(mode), settings.Quality == mode))
                    {
                        settings.Quality = mode;
                    }
                }
                ImGui.EndCombo();
            }
            Help("Quality preserves more detail. Performance draws fewer pixels before reconstruction.");
        }
        ImGui.TextDisabled($"{rhi.InputWidth} x {rhi.InputHeight}  ->  {rhi.Window.Width} x {rhi.Window.Height}");
        Help("The left size is rendered by the GPU; the right size is the displayed image. Resize the window to change output size.");
        bool generation = settings.GeneratedFrames > 0;
        ImGui.BeginDisabled(!rhi.Capabilities.FrameGeneration);
        if (ImGui.Checkbox(T("DLSS frame generation"), ref generation))
        {
            settings.GeneratedFrames = generation ? 1u : 0;
        }

        Help(rhi.Capabilities.FrameGeneration ? "Adds displayed frames between rendered frames. It does not increase the simulation rate. Reflex reduces input latency." : session.Available(SL.FeatureDLSSG) ? "Needs a larger output window." : "Not supported on this device or backend.");
        ImGui.EndDisabled();
        if (settings.GeneratedFrames > 0)
        {
            if (session.FrameGenerationIssue is not null)
            {
                ImGui.TextColored(Accent, T("Frame generation needs attention; see device details."));
            }
            else
            {
                Hint(string.Format(CultureInfo.InvariantCulture, T("1 rendered + {0} generated ({1}x)"), settings.GeneratedFrames, settings.GeneratedFrames + 1));
            }
        }
        ImGui.SeparatorText(T("Brightness"));
        Slider("Exposure", ref settings.Exposure, -3, 3, "%.1f EV");
        Help("0 EV is the default. +1 EV doubles brightness; -1 EV halves it.");
        if (ImGui.Button(T("Reset daylight")))
        {
            QualityPresets.ResetDaylight(settings);
        }
        ImGui.SameLine();
        if (ImGui.Button(T("Reset view")))
        {
            ResetCamera = true;
        }
    }

    private void CameraControls(RenderSettings settings)
    {
        ImGui.Checkbox(T("Pause objects"), ref settings.PauseAnimation);
        ImGui.Checkbox(T("Lock camera"), ref settings.FixedCamera);
        Hint("Hold right mouse + WASD to move; Q/E down/up; Shift accelerates. F1 hides this panel.");
    }

    private void Advanced(RHI rhi)
    {
        RenderSettings settings = rhi.Settings;
        StreamlineSession session = rhi.Streamline;
        Hint("These controls override the preset. Use Recommended to return to a coherent combination.");
        ImGui.TextUnformatted(T("Reconstruction method"));
        if (ImGui.BeginCombo("##Method", MethodLabel(settings.Reconstruction)))
        {
            foreach (Reconstruction method in Enum.GetValues<Reconstruction>())
            {
                uint? feature = RHI.Feature(method);
                bool supported = feature is null || session.Available(feature.Value);
                ImGui.BeginDisabled(!supported);
                if (ImGui.Selectable(MethodLabel(method), method == settings.Reconstruction))
                {
                    settings.Reconstruction = method;
                    settings.RayTracing = method == Reconstruction.RayReconstruction;
                }
                Help(!supported ? method == Reconstruction.DirectSR && rhi.BackendName == "Vulkan" ? "Requires DirectX 12" : "Not supported on this device or backend." : "Quality preserves more detail. Performance draws fewer pixels before reconstruction.");
                ImGui.EndDisabled();
            }
            ImGui.EndCombo();
        }
        if (settings.Reconstruction == Reconstruction.NIS)
        {
            Slider("Input scale", ref settings.Scale, 0.5f, 1, "%.2f");
        }

        if (settings.Reconstruction == Reconstruction.DirectSR && session.DirectSRVariants.Length > 0)
        {
            ImGui.TextUnformatted(T("DirectSR variant"));
            if (ImGui.BeginCombo("##Variant", session.DirectSRVariants[settings.DirectSRVariant]))
            {
                for (uint i = 0; i < session.DirectSRVariants.Length; i++)
                {
                    if (ImGui.Selectable(session.DirectSRVariants[i], i == settings.DirectSRVariant))
                    {
                        settings.DirectSRVariant = i;
                    }
                }

                ImGui.EndCombo();
            }
        }
        bool raw = settings.RayTracing && settings.Reconstruction != Reconstruction.RayReconstruction;
        ImGui.BeginDisabled(!rhi.RayQuerySupported);
        if (ImGui.Checkbox(T("Raw ray-traced input"), ref raw))
        {
            settings.RayTracing = raw || rhi.Capabilities.RayReconstruction;
            settings.Reconstruction = raw ? Reconstruction.Native : rhi.Capabilities.RayReconstruction ? Reconstruction.RayReconstruction : rhi.Capabilities.Dlss ? Reconstruction.DLSS : Reconstruction.Native;
        }
        Help("For comparing the light transport with denoising off. Visible noise is expected.");
        ImGui.EndDisabled();
        if (rhi.Capabilities.FrameGeneration && settings.GeneratedFrames > 0 && session.MaximumGeneratedFrames > 1)
        {
            ImGui.TextUnformatted(T("Frame multiplier"));
            if (ImGui.BeginCombo("##Multiplier", $"{settings.GeneratedFrames + 1}x"))
            {
                for (uint count = 1; count <= session.MaximumGeneratedFrames; count++)
                {
                    if (ImGui.Selectable($"{count + 1}x", count == settings.GeneratedFrames))
                    {
                        settings.GeneratedFrames = count;
                    }
                }

                ImGui.EndCombo();
            }
        }
        ImGui.TextUnformatted(T("Reflex low latency"));
        ImGui.BeginDisabled(!rhi.Capabilities.Reflex);
        if (ImGui.BeginCombo("##Reflex", ReflexLabel(settings.Reflex)))
        {
            foreach (ReflexMode mode in new[] { ReflexMode.Off, ReflexMode.LowLatency, ReflexMode.LowLatencyWithBoost })
            {
                if (ImGui.Selectable(ReflexLabel(mode), mode == settings.Reflex))
                {
                    settings.Reflex = mode;
                }
            }

            ImGui.EndCombo();
        }
        Help("Boost can increase GPU power use. On is recommended for normal use.");
        ImGui.EndDisabled();
        ImGui.BeginDisabled(!session.Available(SL.FeatureDeepDVC));
        ImGui.Checkbox(T("DeepDVC color boost"), ref settings.DeepDVC);
        Help("Off keeps the natural lighting colors. This enhances saturation after tone mapping.");
        if (settings.DeepDVC)
        {
            Slider("Color intensity", ref settings.Intensity, 0, 1, "%.2f");
            Slider("Saturation boost", ref settings.Saturation, 0, 1, "%.2f");
        }
        ImGui.EndDisabled();
        if (ImGui.TreeNode(T("Daylight controls")))
        {
            Slider("Sun height", ref settings.SunElevation, 10, 80, "%.0f deg");
            Slider("Sun direction", ref settings.SunAzimuth, -180, 180, "%.0f deg");
            Slider("Sun strength", ref settings.SunIntensity, 0, 16, "%.1f");
            Slider("Sky fill", ref settings.SkyIntensity, 0, 2, "%.2f");
            Slider("Warm fill", ref settings.LocalLightIntensity, 0, 8, "%.1f");
            Help("Warm fill is off by default to preserve natural light and shadow.");
            ImGui.BeginDisabled(settings.RayTracing);
            ImGui.Checkbox(T("Contact shading"), ref settings.ContactShadows);
            Help("Adds local occlusion to raster lighting. Ray tracing measures visibility directly.");
            ImGui.EndDisabled();
            ImGui.TreePop();
        }
    }

    private void Information(RHI rhi)
    {
        StreamlineSession session = rhi.Streamline;
        ImGui.TextWrapped(rhi.AdapterName);
        ImGui.TextUnformatted($"{T("Window size")}: {rhi.Window.Width} x {rhi.Window.Height}");
        ImGui.TextUnformatted($"{T("CPU frame")}: {rhi.CpuMilliseconds:F2} ms");
        ImGui.TextUnformatted(rhi.GpuMilliseconds is double gpu ? $"{T("GPU rendering")}: {gpu:F2} ms" : $"{T("GPU rendering")}: {T("Unavailable")}");
        ImGui.TextUnformatted(session.LatencyMilliseconds is double latency ? $"{T("Simulation to GPU end")}: {latency:F2} ms" : $"{T("Simulation to GPU end")}: {T("Unavailable")}");
        Hint("Display FPS is not measured; no value is inferred from the frame multiplier.");
        ImGui.TextWrapped($"{T("Hardware ray queries")}: {rhi.RayQueryStatus}");
        if (ImGui.TreeNode(T("SDK details")))
        {
            ImGui.TextUnformatted($"Streamline: {session.RuntimeVersion}");
            ImGui.TextWrapped($"DLSS-G: {session.FrameGenerationStatus}");
            foreach ((uint id, string name) in StreamlineSession.Features)
            {
                ImGui.TextWrapped($"{name}: {(session.Available(id) ? T("Available") : session.Unavailable[id])}");
            }

            ImGui.TreePop();
        }
    }

    private void HeaderLine(string first, string second, Vector4 color)
    {
        bool fits = ImGui.CalcTextSize(first).X + ImGui.CalcTextSize(second).X + ImGui.GetStyle().ItemSpacing.X <= ImGui.GetContentRegionAvail().X;
        ImGui.TextColored(color, first);
        if (fits)
        {
            ImGui.SameLine();
        }

        ImGui.TextDisabled(second);
    }
    private void Slider(string label, ref float value, float minimum, float maximum, string format)
    {
        ImGui.TextUnformatted(T(label));
        ImGui.SliderFloat("##" + label, ref value, minimum, maximum, format);
    }

    private void Hint(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Muted);
        ImGui.TextWrapped(T(text));
        ImGui.PopStyleColor();
    }
    private void Help(string text)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled | ImGuiHoveredFlags.DelayShort))
        {
            return;
        }

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24);
        ImGui.TextUnformatted(T(text));
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
    private string PresetLabel(QualityPreset preset) => T(preset switch
    {
        QualityPreset.Recommended => "Recommended",
        QualityPreset.Quality => "Quality first",
        QualityPreset.Performance => "Performance first",
        _ => "Native reference"
    });
    private static string PresetDescription(QualityPreset preset) => preset switch
    {
        QualityPreset.Recommended => "Ray-traced lighting + DLSS Quality + frame generation where supported.",
        QualityPreset.Quality => "Native-resolution anti-aliasing; higher GPU cost.",
        QualityPreset.Performance => "Raster lighting + DLSS Balanced; lower GPU cost.",
        _ => "Turns off upscaling and frame generation for comparison."
    };
    private string QualityLabel(DLSSMode mode) => T(mode switch
    {
        DLSSMode.MaxQuality => "Quality",
        DLSSMode.Balanced => "Balanced",
        DLSSMode.MaxPerformance => "Performance",
        DLSSMode.UltraPerformance => "Ultra performance",
        DLSSMode.DLAA => "Native anti-aliasing",
        _ => "Quality"
    });
    private string MethodLabel(Reconstruction method) => T(method switch
    {
        Reconstruction.Native => "Native + FXAA",
        Reconstruction.DLSS => "DLSS upscaling",
        Reconstruction.DLAA => "DLAA anti-aliasing",
        Reconstruction.RayReconstruction => "Ray Reconstruction",
        Reconstruction.NIS => "NIS spatial upscaling",
        _ => "DirectSR system upscaling"
    });
    private string ReflexLabel(ReflexMode mode) => T(mode switch
    {
        ReflexMode.Off => "Off",
        ReflexMode.LowLatency => "On",
        _ => "On + Boost"
    });
    public void Dispose() => ImGui.DestroyContext(context);
}
