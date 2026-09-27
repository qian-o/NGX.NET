using System.Numerics;
using ImGuiNET;
using Streamline.NET;

namespace Showcase;

internal sealed unsafe class UserInterface : IDisposable
{
    private readonly nint context;
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
        style.WindowPadding = new(16, 16);
        style.ItemSpacing = new(8, 9);
        style.Colors[(int)ImGuiCol.WindowBg] = new(0.035f, 0.045f, 0.065f, 0.94f);
        style.Colors[(int)ImGuiCol.Header] = new(0.08f, 0.28f, 0.35f, 1);
        style.Colors[(int)ImGuiCol.CheckMark] = new(0.25f, 0.85f, 0.7f, 1);
        ImFontConfigPtr font = ImGuiNative.ImFontConfig_ImFontConfig();
        try
        {
            font.SizePixels = 17;
            io.Fonts.AddFontDefault(font);
        }
        finally { font.Destroy(); }
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
        FontWidth = width;
        FontHeight = height;
        FontPixels = new ReadOnlySpan<byte>(pixels, width * height * bytesPerPixel).ToArray();
        io.Fonts.SetTexID(1);
    }

    public void Build(RHI rhi, float delta)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new(rhi.Window.Width, rhi.Window.Height);
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = Math.Max(delta, 1e-4f);
        ImGui.NewFrame();
        ImGui.SetNextWindowPos(new(18, 18), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new(Math.Min(370, rhi.Window.Width - 36), Math.Min(810, rhi.Window.Height - 36)), ImGuiCond.Always);
        if (ImGui.Begin("Streamline.NET Showcase", ImGuiWindowFlags.NoCollapse))
        {
            RenderSettings settings = rhi.Settings;
            StreamlineSession session = rhi.Streamline;
            ImGui.TextColored(new(0.3f, 0.9f, 0.75f, 1), rhi.BackendName);
            ImGui.TextWrapped(rhi.AdapterName);
            ImGui.SeparatorText("Image reconstruction");
            if (ImGui.BeginCombo("Method", Label(settings.Reconstruction)))
            {
                foreach (Reconstruction method in Enum.GetValues<Reconstruction>())
                {
                    uint? feature = RHI.Feature(method);
                    bool supported = feature is null || session.Available(feature.Value);
                    ImGui.BeginDisabled(!supported);
                    if (ImGui.Selectable(Label(method), method == settings.Reconstruction))
                    {
                        settings.Reconstruction = method;
                        if (method == Reconstruction.RayReconstruction)
                        {
                            settings.RayTracing = true;
                        }
                    }
                    ImGui.EndDisabled();
                    if (!supported && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip(session.Unavailable[feature!.Value]);
                    }
                }
                ImGui.EndCombo();
            }
            if (settings.Reconstruction is Reconstruction.DLSS or Reconstruction.RayReconstruction)
            {
                if (ImGui.BeginCombo("Quality", settings.Quality.ToString()))
                {
                    foreach (DLSSMode mode in new[] { DLSSMode.MaxQuality, DLSSMode.Balanced, DLSSMode.MaxPerformance, DLSSMode.UltraPerformance, DLSSMode.DLAA })
                    {
                        if (ImGui.Selectable(mode.ToString(), settings.Quality == mode))
                        {
                            settings.Quality = mode;
                        }
                    }

                    ImGui.EndCombo();
                }
            }
            if (settings.Reconstruction == Reconstruction.NIS)
            {
                ImGui.SliderFloat("Input scale", ref settings.Scale, 0.5f, 1);
            }

            if (settings.Reconstruction == Reconstruction.DirectSR && ImGui.BeginCombo("Variant", session.DirectSRVariants[settings.DirectSRVariant]))
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
            ImGui.SliderFloat("Exposure (EV)", ref settings.Exposure, -4, 4);
            ImGui.Text($"Input {rhi.InputWidth} x {rhi.InputHeight}");
            ImGui.Text($"Output {rhi.Window.Width} x {rhi.Window.Height}");
            ImGui.TextDisabled("Resize the window to change output size.");
            ImGui.SeparatorText("Ray tracing and latency");
            ImGui.BeginDisabled(!rhi.RayQuerySupported || settings.Reconstruction == Reconstruction.RayReconstruction);
            ImGui.Checkbox("Ray-traced indirect light / reflections", ref settings.RayTracing);
            ImGui.EndDisabled();
            if (settings.RayTracing && settings.Reconstruction != Reconstruction.RayReconstruction)
            {
                ImGui.TextWrapped("Raw low-sample ray-traced input. Choose Ray Reconstruction to denoise it.");
            }

            ImGui.TextWrapped($"Hardware Ray Query: {rhi.RayQueryStatus}");

            bool fgAvailable = session.Available(SL.FeatureDLSSG) && Math.Min(rhi.Window.Width, rhi.Window.Height) >= session.MinimumFGDimension;
            ImGui.BeginDisabled(!fgAvailable);
            if (ImGui.BeginCombo("Frame generation", settings.GeneratedFrames == 0 ? "Off" : $"{settings.GeneratedFrames + 1}x"))
            {
                if (ImGui.Selectable("Off", settings.GeneratedFrames == 0))
                {
                    settings.GeneratedFrames = 0;
                }

                for (uint i = 1; i <= session.MaximumGeneratedFrames; i++)
                {
                    if (ImGui.Selectable($"{i + 1}x", settings.GeneratedFrames == i))
                    {
                        settings.GeneratedFrames = i;
                    }
                }

                ImGui.EndCombo();
            }
            ImGui.EndDisabled();
            ImGui.BeginDisabled(!session.Available(SL.FeatureReflex));
            if (ImGui.BeginCombo("Reflex", settings.Reflex.ToString()))
            {
                foreach (ReflexMode mode in new[] { ReflexMode.Off, ReflexMode.LowLatency, ReflexMode.LowLatencyWithBoost })
                {
                    if (ImGui.Selectable(mode.ToString(), settings.Reflex == mode))
                    {
                        settings.Reflex = mode;
                    }
                }

                ImGui.EndCombo();
            }
            ImGui.EndDisabled();
            ImGui.SeparatorText("Color and scene");
            ImGui.BeginDisabled(!session.Available(SL.FeatureDeepDVC));
            ImGui.Checkbox("DeepDVC", ref settings.DeepDVC);
            if (settings.DeepDVC)
            {
                ImGui.SliderFloat("Intensity", ref settings.Intensity, 0, 1);
                ImGui.SliderFloat("Saturation", ref settings.Saturation, 0, 1);
            }
            ImGui.EndDisabled();
            ImGui.Checkbox("Pause object animation", ref settings.PauseAnimation);
            ImGui.Checkbox("Fixed camera for comparison", ref settings.FixedCamera);
            if (ImGui.Button("Reset camera"))
            {
                ResetCamera = true;
            }

            ImGui.TextWrapped("Hold right mouse + WASD to move. Q/E down/up. Shift accelerates.");
            ImGui.SeparatorText("Measurements");
            ImGui.Text($"Rendered: {rhi.RenderFps:F1} FPS");
            ImGui.Text("Displayed: unavailable");
            ImGui.Text($"CPU frame: {rhi.CpuMilliseconds:F2} ms");
            ImGui.Text(rhi.GpuMilliseconds is double gpu ? $"GPU render: {gpu:F2} ms" : "GPU render: unavailable");
            ImGui.Text(session.LatencyMilliseconds is double latency ? $"Simulation to GPU end: {latency:F2} ms" : "Latency: unavailable");
            ImGui.Text($"Frame generation: {session.FrameGenerationStatus}");
            ImGui.Text($"Streamline: {session.RuntimeVersion}");
            if (ImGui.TreeNode("Feature availability"))
            {
                foreach ((uint id, string name) in StreamlineSession.Features)
                {
                    ImGui.TextWrapped($"{name}: {(session.Available(id) ? "Available" : session.Unavailable[id])}");
                }

                ImGui.TreePop();
            }
        }
        ImGui.End();
        ImGui.Render();
    }

    private static string Label(Reconstruction method) => method switch
    {
        Reconstruction.Native => "Native + FXAA",
        Reconstruction.DLSS => "DLSS Super Resolution",
        Reconstruction.DLAA => "DLAA",
        Reconstruction.RayReconstruction => "DLSS Ray Reconstruction",
        _ => method.ToString()
    };
    public void Dispose() => ImGui.DestroyContext(context);
}
